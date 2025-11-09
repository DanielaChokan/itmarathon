import { useState } from "react";
import { useParams } from "react-router";
import ParticipantCard from "@components/common/participant-card/ParticipantCard";
import ParticipantDetailsModal from "@components/common/modals/participant-details-modal/ParticipantDetailsModal";
import type { Participant } from "../../../types/api.ts";
import {
  MAX_PARTICIPANTS_NUMBER,
  generateParticipantLink,
} from "@utils/general";
import { type ParticipantsListProps, type PersonalInformation } from "./types";
import "./ParticipantsList.scss";

const ParticipantsList = ({ participants, onDeleteUser }: ParticipantsListProps) => {
  const { userCode } = useParams();
  const [selectedParticipant, setSelectedParticipant] =
    useState<PersonalInformation | null>(null);

  const admin = participants?.find((participant) => participant?.isAdmin);
  const restParticipants = participants?.filter(
    (participant) => !participant?.isAdmin,
  );

  const isParticipantsMoreThanTen = participants.length > 10;

  const handleInfoButtonClick = (participant: Participant) => {
    const personalInfoData: PersonalInformation = {
      firstName: participant.firstName,
      lastName: participant.lastName,
      phone: participant.phone,
      deliveryInfo: participant.deliveryInfo,
      email: participant.email,
      link: generateParticipantLink(participant.userCode),
    };
    setSelectedParticipant(personalInfoData);
  };

  const handleDeleteButtonClick = (participant: Participant) => {
    const fullName = `${participant.firstName} ${participant.lastName}`;
    
    if (window.confirm(`Are you sure you want to remove ${fullName} from the room?`)) {
      onDeleteUser(participant.id);
    }
  };

  const handleModalClose = () => setSelectedParticipant(null);

  return (
    <div
      className={`participant-list ${isParticipantsMoreThanTen ? "participant-list--shift-bg-image" : ""}`}
    >
      <div
        className={`participant-list__content ${isParticipantsMoreThanTen ? "participant-list__content--extra-padding" : ""}`}
      >
        <div className="participant-list-header">
          <h3 className="participant-list-header__title">Who’s Playing?</h3>

          <span className="participant-list-counter__current">
            {participants?.length ?? 0}/
          </span>

          <span className="participant-list-counter__max">
            {MAX_PARTICIPANTS_NUMBER}
          </span>
        </div>

        <div className="participant-list__cards">
          {admin ? (
            <ParticipantCard
              key={admin?.id}
              firstName={admin?.firstName}
              lastName={admin?.lastName}
              isCurrentUser={userCode === admin?.userCode}
              isAdmin={admin?.isAdmin}
              isCurrentUserAdmin={userCode === admin?.userCode}
              adminInfo={`${admin?.phone}${admin?.email ? `\n${admin?.email}` : ""}`}
              participantLink={generateParticipantLink(admin?.userCode)}
            />
          ) : null}

          {restParticipants?.map((user) => (
            <ParticipantCard
              key={user?.id}
              firstName={user?.firstName}
              lastName={user?.lastName}
              isCurrentUser={userCode === user?.userCode}
              isCurrentUserAdmin={userCode === admin?.userCode}
              participantLink={generateParticipantLink(user?.userCode)}
              onInfoButtonClick={
                userCode === admin?.userCode && userCode !== user?.userCode
                  ? () => handleInfoButtonClick(user)
                  : undefined
              }
              onDeleteButtonClick={
                userCode === admin?.userCode && userCode !== user?.userCode
                  ? () => handleDeleteButtonClick(user)
                  : undefined
              }
            />
          ))}
        </div>

        {selectedParticipant ? (
          <ParticipantDetailsModal
            isOpen={!!selectedParticipant}
            onClose={handleModalClose}
            personalInfoData={selectedParticipant}
          />
        ) : null}
      </div>
    </div>
  );
};

export default ParticipantsList;
