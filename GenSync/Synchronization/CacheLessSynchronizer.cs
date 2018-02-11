using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GenSync.EntityRelationManagement;
using GenSync.EntityRepositories;
using GenSync.InitialEntityMatching;
using GenSync.Logging;
using GenSync.ProgressReport;
using GenSync.Synchronization.StateCreationStrategies;
using GenSync.Synchronization.StateFactories;
using GenSync.Synchronization.States;
using GenSync.Utilities;
using log4net;

namespace GenSync.Synchronization
{
  class CacheLessSynchronizer<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext, TAMatchData, TBMatchData, TAtypeStateToken, TBtypeStateToken>
  {
    private static readonly ILog s_logger = LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);

    private readonly IExtendedVersionAwareEntityRepository<TAtypeEntityId, TAtypeEntityVersion, TBtypeEntityId, TBtypeEntityVersion, TContext> _aRepository;
    private readonly IVersionAwareEntityRepository<TBtypeEntityId, TBtypeEntityVersion, TContext> _bRepository;
    private readonly IEqualityComparer<TAtypeEntityVersion> _atypeVersionComparer;
    private readonly IEqualityComparer<TBtypeEntityVersion> _btypeVersionComparer;
    private readonly IEqualityComparer<TAtypeEntityId> _atypeIdComparer;
    private readonly IEqualityComparer<TBtypeEntityId> _btypeIdComparer;
    private readonly IInitialSyncStateCreationStrategy<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> _initialSyncStateCreationStrategy;
    private IReadOnlyEntityRepository<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TContext> _aTypeRepository;
    private IReadOnlyEntityRepository<TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> _bTypeRepository;
    private readonly IChunkedExecutor _chunkedExecutor;
    private readonly int? _chunkSize;
    private readonly IMatchDataFactory<TAtypeEntity, TAMatchData> _aMatchDataFactory;
    private readonly IExtendedMatchDataFactory<TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TAtypeEntityId, TAtypeEntityVersion, TBMatchData> _bMatchDataFactory;
    private readonly IInitialEntityMatcher<TAtypeEntityId, TAtypeEntityVersion, TAMatchData, TBtypeEntityId, TBtypeEntityVersion, TBMatchData> _initialEntityMatcher;
    private readonly IEntityRelationDataFactory<TAtypeEntityId, TAtypeEntityVersion, TBtypeEntityId, TBtypeEntityVersion> _entityRelationDataFactory;
    private readonly ISynchronizationInterceptorFactory<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> _synchronizationInterceptorFactory;
    private readonly IEntitySyncStateFactory<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> _syncStateFactory;
    private readonly EntitySyncStateChunkCreator<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext> _entitySyncStateChunkCreator;
    private readonly ITotalProgressFactory _totalProgressFactory;
    private readonly IFullEntitySynchronizationLoggerFactory<TAtypeEntity, TBtypeEntity> _fullEntitySynchronizationLoggerFactory;

    public async void Synchronize(ISynchronizationLogger logger, TContext synchronizationContext)
    {
      var aVersions = (await _aRepository.GetAllVersions(synchronizationContext, logger.AGetVersionsEntityLogger)).ToArray();
      var bVersionsById = (await _bRepository.GetAllVersions(null, synchronizationContext, logger.BGetVersionsEntityLogger)).ToDictionary(b => b.Id, b => b.Version, _btypeIdComparer);

      var entitySynchronizationStates = new List<IEntitySyncState<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>>();
      var aAdded = new Dictionary<TAtypeEntityId, TAtypeEntityVersion>();

      var aDeltaLogInfo = new VersionDeltaLoginInformation();
      var bDeltaLogInfo = new VersionDeltaLoginInformation();

      foreach (var aVersion in aVersions)
      {
        if (aVersion.RelationOrNull == null)
        {
          aAdded.Add(aVersion.Id, aVersion.Version);
        }
        else
        {

          var aChanged = _atypeVersionComparer.Equals(aVersion.Version, aVersion.RelationOrNull.AtypeVersion);

          if (bVersionsById.TryGetValue(aVersion.RelationOrNull.BtypeId, out var bVersionOrNull))
          {
            bVersionsById.Remove(aVersion.RelationOrNull.BtypeId);

            if (_btypeVersionComparer.Equals(aVersion.RelationOrNull.BtypeVersion, bVersionOrNull))
            {
              if (aChanged)
              {
                entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Changed_Unchanged(aVersion.RelationOrNull, aVersion.Version));
                aDeltaLogInfo.IncChanged();
              }
              else
              {
                entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Unchanged_Unchanged(aVersion.RelationOrNull));
                aDeltaLogInfo.IncUnchanged();
              }
              bDeltaLogInfo.IncUnchanged();
            }
            else
            {
              if (aChanged)
              {
                entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Changed_Changed(aVersion.RelationOrNull, aVersion.Version, bVersionOrNull));
                aDeltaLogInfo.IncChanged();
              }
              else
              {
                entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Unchanged_Changed(aVersion.RelationOrNull, bVersionOrNull));
                aDeltaLogInfo.IncUnchanged();
              }
              bDeltaLogInfo.IncChanged();
            }
          }
          else
          {
            if (aChanged)
            {
              entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Changed_Deleted(aVersion.RelationOrNull, aVersion.Version));
              aDeltaLogInfo.IncChanged();
            }
            else
            {
              entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Unchanged_Deleted(aVersion.RelationOrNull));
              aDeltaLogInfo.IncUnchanged();
            }
            bDeltaLogInfo.IncDeleted();
          }
        }
      }

      using (var interceptor = _synchronizationInterceptorFactory.Create())
      {
        using (var aEntityContainer = new EntityContainer<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TContext>(_aTypeRepository, _atypeIdComparer, _chunkSize, _chunkedExecutor))
        using (var bEntityContainer = new EntityContainer<TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>(_bTypeRepository, _btypeIdComparer, _chunkSize, _chunkedExecutor))
        {
          var knownBtypes = new HashSet<TBtypeEntityId>(_btypeIdComparer);

          TBMatchData CreateBtypeMatchData(EntityWithId<TBtypeEntityId, TBtypeEntity> entity)
          {
            var transformed = _bMatchDataFactory.CreateMatchData(entity.Entity);
            if (transformed.IsKnown)
              knownBtypes.Add(entity.Id);
            return transformed.MatchData;
          }

          var matchingEntites = _initialEntityMatcher.FindMatchingEntities(
            _entityRelationDataFactory,
            await aEntityContainer.GetTransformedEntities(aAdded.Keys, logger.ALoadEntityLogger, synchronizationContext, e => _aMatchDataFactory.CreateMatchData(e.Entity)),
            await bEntityContainer.GetTransformedEntities(bVersionsById.Keys, logger.BLoadEntityLogger, synchronizationContext, CreateBtypeMatchData),
            aAdded,
            bVersionsById);

          foreach (var knownEntityRelationData in matchingEntites)
          {
            aAdded.Remove(knownEntityRelationData.AtypeId);
            bVersionsById.Remove(knownEntityRelationData.BtypeId);
            entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Unchanged_Unchanged(knownEntityRelationData));
            aDeltaLogInfo.IncUnchanged();
            bDeltaLogInfo.IncUnchanged();
          }

          aDeltaLogInfo.IncAdded(aAdded.Count);
          entitySynchronizationStates.AddRange(aAdded.Select(a => _initialSyncStateCreationStrategy.CreateFor_Added_NotExisting(a.Key, a.Value)));

          foreach (var b in bVersionsById)
          {
            if (knownBtypes.Contains(b.Key))
            {
              aDeltaLogInfo.IncDeleted();

              // since teh relation is attached to a and a was deleted, it cannot be determined if b is changed or unchanged. Therefore changed is assumed
              entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_Deleted_Changed(relationData, b.Value));
              bDeltaLogInfo.IncChanged();
            }
            else
            {
              entitySynchronizationStates.Add(_initialSyncStateCreationStrategy.CreateFor_NotExisting_Added(b.Key, b.Value));
              bDeltaLogInfo.IncAdded(1);
            }
          }

          var entitySynchronizationContexts = entitySynchronizationStates.Select(s => new EntitySyncStateContext<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext>(s)).ToList();

          interceptor.TransformInitialCreatedStates(entitySynchronizationContexts, _syncStateFactory);


          s_logger.InfoFormat("Atype delta: {0}", aDeltaLogInfo);
          s_logger.InfoFormat("Btype delta: {0}", bDeltaLogInfo);
          logger.LogDeltas(aDeltaLogInfo, bDeltaLogInfo);


          //try
          //{
          //  var chunks = _entitySyncStateChunkCreator.CreateChunks(entitySynchronizationContexts, _atypeIdComparer, _btypeIdComparer).ToArray();
          //  var entitySynchronizationLoggerFactory = new SynchronizationLoggerBoundEntitySynchronizationLoggerFactory<TAtypeEntity, TBtypeEntity>(logger, _fullEntitySynchronizationLoggerFactory);

          //  using (var totalProgress = _totalProgressFactory.Create())
          //  {
          //    totalProgress.NotifyWork(chunks.Aggregate(0, (acc, c) => acc + c.AEntitesToLoad.Count + c.BEntitesToLoad.Count), chunks.Length);

          //    foreach ((var aEntitesToLoad, var bEntitesToLoad, var currentBatch) in chunks)
          //    {
          //      var chunkLogger = totalProgress.StartChunk();

          //      IReadOnlyDictionary<TAtypeEntityId, TAtypeEntity> aEntitiesById;
          //      using (chunkLogger.StartARepositoryLoad(aEntitesToLoad.Count))
          //      {
          //        aEntitiesById = await aEntityContainer.GetEntities(aEntitesToLoad, logger.ALoadEntityLogger, synchronizationContext);
          //      }

          //      IReadOnlyDictionary<TBtypeEntityId, TBtypeEntity> bEntitiesById;
          //      using (chunkLogger.StartBRepositoryLoad(bEntitesToLoad.Count))
          //      {
          //        bEntitiesById = await bEntityContainer.GetEntities(bEntitesToLoad, logger.BLoadEntityLogger, synchronizationContext);
          //      }

          //      currentBatch.ForEach(s => s.FetchRequiredEntities(aEntitiesById, bEntitiesById));
          //      currentBatch.ForEach(s => s.Resolve());

          //      // since resolve may change to an new state, required entities have to be fetched again.
          //      // an state is allowed only to resolve to another state, if the following states requires equal or less entities!
          //      currentBatch.ForEach(s => s.FetchRequiredEntities(aEntitiesById, bEntitiesById));

          //      var aJobs = new JobList<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity>();
          //      var bJobs = new JobList<TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity>();

          //      currentBatch.ForEach(s => s.AddSyncronizationJob(aJobs, bJobs, entitySynchronizationLoggerFactory, synchronizationContext));

          //      totalAJobs = totalAJobs.Add(aJobs.Count);
          //      totalBJobs = totalBJobs.Add(bJobs.Count);

          //      try
          //      {
          //        using (var progress = chunkLogger.StartProcessing(aJobs.TotalJobCount + bJobs.TotalJobCount))
          //        {
          //          await _atypeWriteRepository.PerformOperations(aJobs.CreateJobs, aJobs.UpdateJobs, aJobs.DeleteJobs, progress, synchronizationContext);
          //          await _btypeWriteRepository.PerformOperations(bJobs.CreateJobs, bJobs.UpdateJobs, bJobs.DeleteJobs, progress, synchronizationContext);
          //        }

          //        currentBatch.ForEach(s => s.NotifyJobExecuted());
          //      }
          //      catch (Exception x)
          //      {
          //        if (_exceptionHandlingStrategy.DoesGracefullyAbortSynchronization(x))
          //        {
          //          entitySynchronizationContexts.ForEach(s => s.Abort());
          //          SaveNewRelations(entitySynchronizationContexts, saveNewRelations);
          //        }
          //        throw;
          //      }
          //    }
          //  }
          //}
          //finally
          //{
          //  s_logger.InfoFormat($"A repository jobs: {totalAJobs}");
          //  s_logger.InfoFormat($"B repository jobs: {totalBJobs}");
          //  logger.LogJobs(totalAJobs.ToString(), totalBJobs.ToString());
          //}

          //SaveNewRelations(entitySynchronizationContexts, saveNewRelations);


        }



      }







    }



  }
}
