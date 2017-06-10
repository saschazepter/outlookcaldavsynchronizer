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
using CalDavSynchronizer.DataAccess;
using CalDavSynchronizer.Implementation.ComWrappers;
using DDay.iCal;
using GenSync.EntityRelationManagement;
using GenSync.Synchronization.StateCreationStrategies;
using GenSync.Synchronization.StateFactories;
using GenSync.Synchronization.States;

namespace CalDavSynchronizer.Implementation.Events
{
  public class SafetyMarginSyncStateCreationStrategyDecorator : IInitialSyncStateCreationStrategy<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext>
  {
    private readonly IEntitySyncStateFactory<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> _stateFactory;
    private readonly IInitialSyncStateCreationStrategy<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> _inner;

    public SafetyMarginSyncStateCreationStrategyDecorator(IEntitySyncStateFactory<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> stateFactory, IInitialSyncStateCreationStrategy<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> inner)
    {
      _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
      _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Unchanged_Unchanged(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData)
    {
      return _inner.CreateFor_Unchanged_Unchanged(knownData);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Changed_Changed(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData, DateTime newA, string newB)
    {
      return _inner.CreateFor_Changed_Changed(knownData, newA, newB);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Deleted_Deleted(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData)
    {
      return _inner.CreateFor_Deleted_Deleted(knownData);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Changed_Unchanged(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData, DateTime newA)
    {
      return _inner.CreateFor_Changed_Unchanged(knownData, newA);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Unchanged_Changed(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData, string newB)
    {
      return _inner.CreateFor_Unchanged_Changed(knownData, newB);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Changed_Deleted(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData, DateTime newA)
    {
      return IsInFilterSafetyMargin(knownData.AtypeId) 
        ? _stateFactory.Create_DoNothing(knownData) 
        : _inner.CreateFor_Changed_Deleted(knownData, newA);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Deleted_Changed(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData, string newB)
    {
      return _inner.CreateFor_Deleted_Changed(knownData, newB);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Deleted_Unchanged(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData)
    {
      return _inner.CreateFor_Deleted_Unchanged(knownData);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Unchanged_Deleted(IEntityRelationData<AppointmentId, DateTime, WebResourceName, string> knownData)
    {
      return IsInFilterSafetyMargin(knownData.AtypeId) 
        ? _stateFactory.Create_DoNothing(knownData) 
        : _inner.CreateFor_Unchanged_Deleted(knownData);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_Added_NotExisting(AppointmentId aId, DateTime newA)
    {
      return IsInFilterSafetyMargin(aId) 
        ? _stateFactory.Create_Discard() 
        : _inner.CreateFor_Added_NotExisting(aId, newA);
    }

    public IEntitySyncState<AppointmentId, DateTime, IAppointmentItemWrapper, WebResourceName, string, IICalendar, IEventSynchronizationContext> CreateFor_NotExisting_Added(WebResourceName bId, string newB)
    {
      return _inner.CreateFor_NotExisting_Added(bId, newB);
    }
    
    bool IsInFilterSafetyMargin(AppointmentId id)
    {
      // Somehow the start and end date must be know here to decide that
      throw new NotImplementedException();
    }
  }
}
