import { useEffect } from "react";
import { useParams } from "react-router";
import type {
  GetParticipantsResponse,
  GetRoomResponse,
  DrawRoomResponse,
} from "../../types/api.ts";
import Loader from "@components/common/loader/Loader.tsx";
import { useFetch } from "@hooks/useFetch.ts";
import useToaster from "@hooks/useToaster.ts";
import { BASE_API_URL } from "@utils/general.ts";
import RoomPageContent from "./room-page-content/RoomPageContent.tsx";
import { ROOM_PAGE_TITLE } from "./utils.ts";
import "./RoomPage.scss";

const RoomPage = () => {
  const { showToast } = useToaster();
  const { userCode } = useParams();

  useEffect(() => {
    document.title = ROOM_PAGE_TITLE;
  }, []);

  const {
    data: roomDetails,
    isLoading: isLoadingRoomDetails,
    fetchData: fetchRoomDetails,
  } = useFetch<GetRoomResponse>(
    {
      url: `${BASE_API_URL}/api/rooms?userCode=${userCode}`,
      method: "GET",
      headers: { "Content-Type": "application/json" },
      onError: () => {
        showToast("Something went wrong. Try again.", "error", "large");
      },
    },
    true,
  );

  const {
    data: participants,
    isLoading: isLoadingParticipants,
    fetchData: fetchParticipants,
  } = useFetch<GetParticipantsResponse>({
    url: `${BASE_API_URL}/api/users?userCode=${userCode}`,
    method: "GET",
    headers: { "Content-Type": "application/json" },
    onError: () => {
      showToast("Something went wrong. Try again.", "error", "large");
    },
  });

  const { fetchData: fetchRandomize, isLoading: isRandomizing } =
    useFetch<DrawRoomResponse>(
      {
        url: `${BASE_API_URL}/api/rooms/draw?userCode=${userCode}`,
        method: "POST",
        headers: { "Content-Type": "application/json" },
        onSuccess: () => {
          showToast(
            "Success! All participants are matched.\nLet the gifting magic start!",
            "success",
            "large",
          );
          fetchRoomDetails();
          fetchParticipants();
        },
        onError: () => {
          showToast("Something went wrong. Try again.", "error", "large");
        },
      },
      false,
    );

  const { fetchData: deleteUser, isLoading: isDeleting } =
    useFetch<void>(
      {
        url: "", // URL буде встановлено динамічно
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
        onSuccess: () => {
          showToast("User successfully removed from the room", "success", "large");
          fetchParticipants();
        },
        onError: () => {
          showToast("Something went wrong. Try again.", "error", "large");
        },
      },
      false,
    );

  const handleDrawNames = () => {
    fetchRandomize();
  };

  const handleDeleteUser = (userId: number) => {
    deleteUser(undefined, {
      url: `${BASE_API_URL}/api/users/${userId}?userCode=${userCode}`,
    });
  };

  useEffect(() => {
    if (userCode) {
      fetchRoomDetails();
      fetchParticipants();
    }
  }, [userCode]);   

  const isLoading =
    isLoadingRoomDetails || isLoadingParticipants || isRandomizing ||
    isDeleting;

  if (!userCode) {
    return null;
  }

  return (
    <main className="room-page">
      {isLoading ? <Loader /> : null}

      {!isLoading && roomDetails && participants ? (
        <RoomPageContent
          roomDetails={roomDetails}
          participants={participants}
          onDrawNames={handleDrawNames}
          onDeleteUser={handleDeleteUser}
        />
      ) : null}
    </main>
  );
};

export default RoomPage;
