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
import { Component, OnInit, OnDestroy, Input } from "@angular/core";

import { Subject } from "rxjs";
import { takeUntil, filter } from "rxjs/operators";

import { environment } from '../../../environments/environment';
import { User, EntryFile } from "../../core/dtos";
import * as Dashboard from "@uppy/dashboard";
import * as Tus from "@uppy/tus";
import * as Uppy from '@uppy/core';
import { EntriesService } from "../entries.service";
import { NotificationService } from "../../core/notification.service";
import { DialogDeleteConfirmComponent } from "../../core/components/dialog-delete-confirm.component";
import { MatDialog } from "@angular/material/dialog";
import { DialogFilePreviewComponent } from "./dialog-file-preview.component";

@Component({
	selector: "entry-files",
	templateUrl: "entry-files.component.html"
})
export class EntryFilesComponent implements OnInit, OnDestroy {	
	@Input()
	entryId: string;

	@Input()
	user: User;

  @Input()
  files: EntryFile[];

  @Input()
  forComment: boolean;

  @Input()
  commentId: number;

  @Input()
  isEdit: boolean;

  isFilesModalShown: boolean = false;
  isFileViewerModalShown: boolean = false;
	onDestroy$ = new Subject<void>()
  uppyEvent = new Subject<[string, any, any, any]>()
  uppy = Uppy();

	uppyConfig = {
      autoProceed: true,
		restrictions: {
      maxFileSize: 200 * 1024 * 1024,
      allowedFileTypes: ['image/*', 'audio/*', 'video/*', '.heic', '.heif', 'text/plain', 'application/pdf', 'application/vnd.ms-*', 'application/vnd.openxmlformats-officedocument.*', '.doc', '.xls', 'application/x-compressed', 'application/x-zip-compressed', 'application/zip', 'multipart/x-zip', 'application/octet-stream']
    },
    locale: {
      strings: {
        youCanOnlyUploadFileTypes: 'You can only upload images, videos, audio, office or zip files'
      }
    }
	}

  uppyPlugins = [
    ["Dashboard",
      Dashboard, {
			inline: true,
			replaceTargetContent: true,
			showProgressDetails: true,
			note: 'Images, videos, audio, office, zip files only.',
			width: 1110,
      browserBackButtonClose: true,
      showLinkToFileUploadResult: false
    }],
    ["Tus",
      Tus, {
      endpoint: environment.apiRootUri + 'tus-file-upload',
      removeFingerprintOnSuccess: true,
      limit: 1
     }]
  ]

  get commentFiles(): EntryFile[] {
    return this.files.filter(f => f.onComment == true);
  }
  get entryFiles(): EntryFile[] {
    return this.files.filter(f => f.onComment == false);
  }

  constructor(
    private entriesService: EntriesService,
    private notificationService: NotificationService,
    private dialog: MatDialog) {
    this.uppyEvent
      .pipe(takeUntil(this.onDestroy$))
      .pipe(filter((value) => value[0] == 'upload-success'))
      .subscribe(
        ([ev, file, response]) => {
          let newEntryFile = new EntryFile();
          newEntryFile.createdAt = new Date();
          newEntryFile.createdBy = this.user.id;
          newEntryFile.entryId = this.entryId;
          let tusId = response.uploadURL.substring(response.uploadURL.lastIndexOf('/') + 1); // Get the TUS file id the last part of the returned url.
          newEntryFile.fileId = tusId.replace(/(\w{8})(\w{4})(\w{4})(\w{4})(\w{12})/, "$1-$2-$3-$4-$5"); // File id should be a guid.
          newEntryFile.filename = file.name;
          newEntryFile.onComment = this.forComment;
          newEntryFile.type = file.type;
          newEntryFile.createdByName = `${this.user.firstName} ${this.user.lastName}`;
          newEntryFile.size = file.size;
          newEntryFile.isAudioVideoEncoded = false;

          this.files.push(newEntryFile)
        },
        (err) => console.dir(err)
      );
	}

	ngOnInit() {
	}

  showFilesModal() {
    this.isFilesModalShown = true;
    // Set the entryId on the meta data to be sent with each file.
    if (this.forComment) {
      this.uppyConfig["meta"] = { entryId: this.entryId, onComment: true };
    } else {
      this.uppyConfig["meta"] = { entryId: this.entryId };
    }
  }

  onFilesModalHidden() {
    this.isFilesModalShown = false;
  }

  previewFile(file) {
    const dialogRef = this.dialog.open(DialogFilePreviewComponent, {
      data: { file: file, canDelete: this.isEdit }
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === "deleted") {
        this.removeFile(file);
      }
    });
  }

  getThumbnail(file, size) {
    return EntryFile.getThumbnailPath(file, size);
  }
  
  onDeleteFileClick(file) {
    let dialogRef = this.dialog.open(DialogDeleteConfirmComponent);
    dialogRef.afterClosed().subscribe(result => {
      if (result === "true") {
        this.entriesService.deleteEntryFile(this.entryId, file.fileId)
          .subscribe((_) => {
            this.removeFile(file);
          },
            (error: any) => this.notificationService.addDanger(`There was an error trying to delete the file, please try again.
				${error}`));
      }
    });
  }
  
  removeFile(file) {
    this.files = this.files.filter((e => e.fileId !== file.fileId)); // remove the entry file from the array.
  }



	ngOnDestroy() {
		this.onDestroy$.next()
		this.onDestroy$.complete()
	}
}
