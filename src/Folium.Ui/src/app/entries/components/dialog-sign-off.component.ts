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
import { Component, OnInit, Inject, OnDestroy } from "@angular/core"
import { MAT_DIALOG_DATA, MatDialogRef, MatDialog } from '@angular/material/dialog';

import { Subscription } from 'rxjs';

import { User, Entry, EntrySummary, EntryFile, EntryComment } from "../../core/dtos";
import { UserService } from "../../user/user.service";
import { EntriesService } from "../entries.service";
import { NotificationService } from "../../core/notification.service";
import { DialogConfirmComponent } from '../../core/components/dialog-confirm.component';

@Component({
  selector: "dialog-sign-off",
  templateUrl: "dialog-sign-off.component.html"
})
export class DialogSignOffComponent implements OnInit, OnDestroy {

  entry: Entry | EntrySummary;
  files: EntryFile[];

  newComment: string;
  newCommentFiles: EntryFile[] = [];
  signedInUser: User;

  private signedInUser$: Subscription;

  constructor(
    private userService: UserService,
    private entriesService: EntriesService,
    private notificationService: NotificationService,
		@Inject(MAT_DIALOG_DATA) private data: any,
    private dialogRef: MatDialogRef<DialogSignOffComponent>,
    private dialog: MatDialog) {
    this.entry = data.entry;
    this.files = data.files;
  }

  ngOnInit() {
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => this.signedInUser = user);
  }

  onSignOff() {
    // Check they are happy.
    let dialogRef = this.dialog.open(DialogConfirmComponent, {
      data: {
        body: `<p>
                Just so you know:
                <ul>
                  <li>Once the Entry is signed off it will become read-only and only comments will be allowed.</li>
                </ul>
              </p>
              <p>Are you sure you want to 'Sign-off' this Entry?</p>`
      },
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === "true") {
        let comment = new EntryComment();
        comment.entryId = this.entry.id;
        comment.createdAt = new Date();
        comment.author = this.signedInUser;
        comment.comment = this.newComment;
        comment.fileIds = this.newCommentFiles.map(f => f.fileId);
        comment.forSignOff = true;
        this.entriesService.signOff(comment)
          .subscribe((newComment: EntryComment) => {
            if (this.entry.hasOwnProperty('comments')) {
              this.entry['comments'].push(newComment);
            }
            if (this.files) {
              // Files may not be populated if we have been given an EntrySummary.
              this.newCommentFiles.forEach(f => f.commentId = newComment.id);
              this.files.push(...this.newCommentFiles);
            }
            this.dialogRef.close(true /* signed off */);
          },
            (error: any) => this.notificationService.addDanger(`There was an error trying to sign-off the entry, please try again.
      ${error}`));
      }
    });   
  }

  onCancelRequest() {
    this.dialogRef.close(false /* not signed off */);
  }

  ngOnDestroy(): void {
    this.signedInUser$.unsubscribe();
  }
}
