import Modal from "../modal/Modal";
import type { ConfirmDeleteModalProps } from "./types";

const ConfirmDeleteModal = ({
  isOpen = false,
  onClose,
  onConfirm,
  participantName,
}: ConfirmDeleteModalProps) => {
  return (
    <Modal
      title="Remove Participant"
      description={`Are you sure you want to remove ${participantName} from the room? This action cannot be undone.`}
      iconName="tree"
      isOpen={isOpen}
      onClose={onClose}
      onConfirm={onConfirm}
      confirmButtonText="Remove"
    >
    </Modal>
  );
};

export default ConfirmDeleteModal;
