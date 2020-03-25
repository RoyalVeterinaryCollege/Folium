/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
import { Component, Inject, ViewChild, ElementRef } from "@angular/core"
import { EntryFile } from "../../core/dtos";
import { MAT_DIALOG_DATA, MatDialogRef, MatDialog } from "@angular/material/dialog";
import { EntriesService } from "../entries.service";
import { NotificationService } from "../../core/notification.service";
import { DialogDeleteConfirmComponent } from "../../core/components/dialog-delete-confirm.component";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { SecurityService } from "../../core/security.service";
import { environment } from '../../../environments/environment';

export interface DialogFilePreviewData {
  file: EntryFile;
  canDelete: boolean;
}

@Component({
    selector: 'dialog-file-preview',
    templateUrl: 'dialog-file-preview.component.html'
})
export class DialogFilePreviewComponent {

  imageLoaded = false;
  _loadedCount = 0;

  playerSources = [];

  @ViewChild("downloadLink", { static: false })
  private downloadLink: ElementRef;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: DialogFilePreviewData,
    public dialogRef: MatDialogRef<DialogFilePreviewComponent>,
    private entriesService: EntriesService,
    private notificationService: NotificationService,
    private dialog: MatDialog,
    private http: HttpClient,
    private securityService: SecurityService) { }

  ngOnInit() {
    if (this.data.file.isAudioVideoEncoded) {
      this.playerSources = [
        {
          src: this.getFileRequestPath(this.data.file) + '?format=mp4',
          type: 'video/mp4'
        },
        {
          src: this.getFileRequestPath(this.data.file) + '?format=webm',
          type: 'video/webm'
        }
      ]
    }
  }

  getThumbnail(file, size) {
    return EntryFile.getThumbnailPath(file, size);
  }

  onDeleteFileClick() {
    if (!this.data.canDelete) return;
    let dialogRef = this.dialog.open(DialogDeleteConfirmComponent);
    dialogRef.afterClosed().subscribe(result => {
      if (result === "true") {
        this.entriesService.deleteEntryFile(this.data.file.entryId, this.data.file.fileId)
          .subscribe((_) => {
            this.dialogRef.close('deleted');
          },
            (error: any) => this.notificationService.addDanger(`There was an error trying to delete the file, please try again.
				${error}`));
      }
    });
  }

  async onDownloadFileClick() {
    const blob = await this.downloadFile(EntryFile.getRequestPath(this.data.file));
    const url = window.URL.createObjectURL(blob);

    const link = this.downloadLink.nativeElement;
    link.href = url;
    link.download = this.data.file.filename;
    link.click();

    window.URL.revokeObjectURL(url);
  }

  getFileRequestPath(file: EntryFile) {
    return environment.apiRootUri + EntryFile.getRequestPath(file);
  }

  async downloadFile(url: string): Promise<Blob> {
    const file = this.http.get(url, {
      headers: new HttpHeaders({
        'Authorization': `Bearer ${this.securityService.authenticationToken}`,
      }), responseType: 'blob'
    }).toPromise();
    return file;
  }

  loaded() {
    // When using a secure image, the first loaded event to fire is for the small gif which is added.
    this._loadedCount++;
    if (this._loadedCount == 1) return;
    this.imageLoaded = true;
  }
}
