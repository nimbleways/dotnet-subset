﻿using System.Threading;
using System.Threading.Tasks;
using CompanyName.MyMeetings.Modules.Meetings.Application.Configuration.Commands;
using CompanyName.MyMeetings.Modules.Meetings.Domain.MeetingGroups;
using MediatR;

namespace CompanyName.MyMeetings.Modules.Meetings.Application.MeetingGroups.SetMeetingGroupExpirationDate
{
    internal class SetMeetingGroupExpirationDateCommandHandler : ICommandHandler<SetMeetingGroupExpirationDateCommand>
    {
        private readonly IMeetingGroupRepository _meetingGroupRepository;

        internal SetMeetingGroupExpirationDateCommandHandler(IMeetingGroupRepository meetingGroupRepository)
        {
            _meetingGroupRepository = meetingGroupRepository;
        }

        public async Task<Unit> Handle(SetMeetingGroupExpirationDateCommand request, CancellationToken cancellationToken)
        {
            var meetingGroup = await _meetingGroupRepository.GetByIdAsync(new MeetingGroupId(request.MeetingGroupId));

            meetingGroup.SetExpirationDate(request.DateTo);

            return Unit.Value;
        }
    }
}