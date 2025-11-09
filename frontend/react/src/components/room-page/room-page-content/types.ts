import type { GetParticipantsResponse, GetRoomResponse } from "../../../types/api.ts";

export interface RoomPageContentProps {
  participants: GetParticipantsResponse;
  roomDetails: GetRoomResponse;
  onDrawNames: () => void;
  onDeleteUser: (userId: number) => void;
}
