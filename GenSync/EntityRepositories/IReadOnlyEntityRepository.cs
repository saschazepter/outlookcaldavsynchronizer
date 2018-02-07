// This file is Part of CalDavSynchronizer (http://outlookcaldavsynchronizer.sourceforge.net/)
// Copyright (c) 2015 Gerhard Zehetbauer
// Copyright (c) 2015 Alexander Nimmervoll
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenSync.Logging;

namespace GenSync.EntityRepositories
{
  /// <summary>
  /// All readoperations that a repository has to support
  /// </summary>
  public interface IReadOnlyEntityRepository<TEntityId, TEntityVersion, TEntity, TContext>
  {
    Task<IEnumerable<EntityVersion<TEntityId, TEntityVersion>>> GetVersions(IEnumerable<IdWithAwarenessLevel<TEntityId>> idsOfEntitiesToQuery, TContext context, IGetVersionsLogger logger);
    Task VerifyUnknownEntities(Dictionary<TEntityId, TEntityVersion> unknownEntites, TContext context);
    Task<IEnumerable<EntityWithId<TEntityId, TEntity>>> Get (ICollection<TEntityId> ids, ILoadEntityLogger logger, TContext context);
    void Cleanup (TEntity entity);
    void Cleanup (IEnumerable<TEntity> entities);
  }


  public interface IExtendedReadOnlyEntityRepository<TEntityId, TEntityVersion, TEntity, TOtherEntityId, TOtherEntityVersion, TContext>
  {
    Task<IEnumerable<EntityVersion<TEntityId, TEntityVersion>>> GetVersions(IEnumerable<IdWithAwarenessLevel<TEntityId>> idsOfEntitiesToQuery, TContext context, IGetVersionsLogger logger);
    Task VerifyUnknownEntities(Dictionary<TEntityId, TEntityVersion> unknownEntites, TContext context);
    Task<IEnumerable<EntityWithId<TEntityId, TEntity>>> Get(ICollection<TEntityId> ids, ILoadEntityLogger logger, TContext context);
    void Cleanup(TEntity entity);
    void Cleanup(IEnumerable<TEntity> entities);
  }

  public interface IExtendedVersionAwareEntityRepository<TEntityId, TEntityVersion, TOtherEntityId, TOtherEntityVersion, TContext>
  {
    Task<IEnumerable<ExtendedEntityVersion<TEntityId, TEntityVersion, TOtherEntityId, TOtherEntityVersion>>> GetAllVersions(TContext context, IGetVersionsLogger logger);
  }

  public class ExtendedEntityVersion<TEntityId, TEntityVersion, TOtherEntityId, TOtherEntityVersion>
  {
    public TEntityId Id { get; }
    public readonly TEntityVersion Version;
    public readonly ExtendedEntityRelation<TEntityVersion, TOtherEntityId, TOtherEntityVersion> RelationOrNull;

    public ExtendedEntityVersion(TEntityVersion version, ExtendedEntityRelation<TEntityVersion, TOtherEntityId, TOtherEntityVersion> relationOrNull, TEntityId id)
    {
      if (id == null) throw new ArgumentNullException(nameof(id));
      Version = version;
      RelationOrNull = relationOrNull;
      Id = id;
    }
  }

  public class ExtendedEntityRelation<TEntityVersion, TOtherEntityId, TOtherEntityVersion>
  {
    public readonly TEntityVersion AtypeVersion;
    public readonly TOtherEntityId BtypeId;
    public readonly TOtherEntityVersion BtypeVersion;

    public ExtendedEntityRelation(TEntityVersion atypeVersion, TOtherEntityId btypeId, TOtherEntityVersion btypeVersion)
    {
      if (atypeVersion == null) throw new ArgumentNullException(nameof(atypeVersion));
      if (btypeId == null) throw new ArgumentNullException(nameof(btypeId));
      if (btypeVersion == null) throw new ArgumentNullException(nameof(btypeVersion));

      AtypeVersion = atypeVersion;
      BtypeId = btypeId;
      BtypeVersion = btypeVersion;
    }
  }
}