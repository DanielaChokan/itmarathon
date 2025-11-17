import { Component, input, output } from '@angular/core';
import { CommonModalTemplate } from '../modal/common-modal-template/common-modal-template';
import { ButtonText, ModalTitle, PictureName } from '../../../app.enum';

@Component({
  selector: 'app-confirm-delete-modal',
  imports: [CommonModalTemplate],
  templateUrl: './confirm-delete-modal.html',
  styleUrl: './confirm-delete-modal.scss',
})
export class ConfirmDeleteModal {
  readonly participantName = input.required<string>();
  readonly closeModal = output<void>();
  readonly confirmDelete = output<void>();

  public readonly headerPictureName = PictureName.Cookie;
  public readonly headerTitle = ModalTitle.RemoveParticipant;
  public readonly buttonText = ButtonText.Remove;

  public onCloseModal(): void {
    this.closeModal.emit();
  }

  public onConfirmDelete(): void {
    this.confirmDelete.emit();
  }
}
