using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenSync.EntityRepositories;
using GenSync.Logging;

namespace GenSync.Synchronization
{
  class CacheLessSynchronizer<TAtypeEntityId, TAtypeEntityVersion, TAtypeEntity, TBtypeEntityId, TBtypeEntityVersion, TBtypeEntity, TContext, TAMatchData, TBMatchData, TAtypeStateToken, TBtypeStateToken>
  {
    private readonly IExtendedVersionAwareEntityRepository<TAtypeEntityId, TAtypeEntityVersion, TBtypeEntityId, TBtypeEntityVersion, TContext> _aRepository;
    private readonly IVersionAwareEntityRepository<TBtypeEntityId, TBtypeEntityVersion, TContext> _bRepository;
    private readonly IEqualityComparer<TAtypeEntityVersion> _atypeVersionComparer;
    private readonly IEqualityComparer<TBtypeEntityVersion> _btypeVersionComparer;
    private readonly IEqualityComparer<TAtypeEntityId> _atypeIdComparer;
    private readonly IEqualityComparer<TBtypeEntityId> _btypeIdComparer;

    public async void Synchronize(ISynchronizationLogger logger, TContext synchronizationContext)
    {
      var aVersions = (await _aRepository.GetAllVersions(synchronizationContext, logger.AGetVersionsEntityLogger)).ToArray();
      var bVersionsById = (await _bRepository.GetAllVersions(null, synchronizationContext, logger.BGetVersionsEntityLogger)).ToDictionary(b => b.Id, _btypeIdComparer);
      
      foreach (var aVersion in aVersions)
      {
        if (aVersion.RelationOrNull == null)
        {
          // A added 
        }
        else
        {

          var aChanged = _atypeVersionComparer.Equals(aVersion.Version, aVersion.RelationOrNull.AtypeVersion);

          if (bVersionsById.TryGetValue(aVersion.RelationOrNull.BtypeId, out var bVersionOrNull))
          {
            bVersionsById.Remove(aVersion.RelationOrNull.BtypeId);

            if (_btypeVersionComparer.Equals(aVersion.RelationOrNull.BtypeVersion, bVersionOrNull.Version))
            {
              // B unchanged
            }
            else
            {
              // B changed
            }
          }
          else
          {
            // B deleted
          }
        }
      }

      var bAddedOrADeleted = bVersionsById;
      



    }



  }
}
