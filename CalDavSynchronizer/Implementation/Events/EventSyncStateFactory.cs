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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalDavSynchronizer.DataAccess;
using CalDavSynchronizer.Implementation.ComWrappers;
using DDay.iCal;
using GenSync;
using GenSync.EntityMapping;
using GenSync.EntityRelationManagement;
using GenSync.Synchronization.StateFactories;
using GenSync.Synchronization.States;

namespace CalDavSynchronizer.Implementation.Events
{
  class EventSyncStateFactory : EntitySyncStateFactory<string, DateTime, AppointmentItemWrapper, WebResourceName, string, IICalendar>
  {
    private readonly string _serverEmailAddress;

    public EventSyncStateFactory (
      IEntityMapper<AppointmentItemWrapper, IICalendar> mapper,
      IEntityRelationDataFactory<string, DateTime, WebResourceName, string> dataFactory, 
      IExceptionLogger exceptionLogger, 
      string serverEmailAddress)
        : base(mapper, dataFactory, exceptionLogger)
    {
      _serverEmailAddress = serverEmailAddress;
    }

    public override IEntitySyncState<string, DateTime, AppointmentItemWrapper, WebResourceName, string, IICalendar> Create_CreateInB (string aId, DateTime aVersion)
    {
      return new EventCreateInB (Environment, aId, aVersion, _serverEmailAddress);
    }
  }
}
