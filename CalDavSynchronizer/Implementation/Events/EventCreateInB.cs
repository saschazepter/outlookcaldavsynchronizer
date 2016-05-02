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
using CalDavSynchronizer.Implementation.Common;
using CalDavSynchronizer.Implementation.ComWrappers;
using DDay.iCal;
using GenSync.Logging;
using GenSync.Synchronization;
using GenSync.Synchronization.States;
using log4net;

namespace CalDavSynchronizer.Implementation.Events
{
  class EventCreateInB : CreateInB<string, DateTime, AppointmentItemWrapper, WebResourceName, string, IICalendar>
  {
    private static readonly ILog s_logger = LogManager.GetLogger (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private readonly string _serverEmailAddress;

    public EventCreateInB (
        EntitySyncStateEnvironment<string, DateTime, AppointmentItemWrapper, WebResourceName, string, IICalendar> environment,
        string aId,
        DateTime aVersion,
        string serverEmailAddress)
        : base (environment, aId, aVersion)
    {
      _serverEmailAddress = serverEmailAddress;
    }

    public override IEntitySyncState<string, DateTime, AppointmentItemWrapper, WebResourceName, string, IICalendar> Resolve ()
    {
      if (_aEntity != null)
      {
        using (var organizerWrapper = GenericComObjectWrapper.Create (_aEntity.Inner.GetOrganizer()))
        {
          var organizerEmail =
              OutlookUtility.GetEmailAdressOrNull (organizerWrapper.Inner, NullEntitySynchronizationLogger.Instance, s_logger) ??
              OutlookUtility.GetSenderEmailAddressOrNull (_aEntity.Inner, NullEntitySynchronizationLogger.Instance, s_logger);

          if (string.Compare(organizerEmail ,_serverEmailAddress, StringComparison.OrdinalIgnoreCase) == 0)
            return _environment.StateFactory.Create_DeleteInAWithNoRetry (_aId, _aVersion);
        }
      }

      return this;
    }
  }
}