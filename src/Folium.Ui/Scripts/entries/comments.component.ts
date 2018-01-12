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
import { Component, Input, PipeTransform, Pipe, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";

import { Subscription } from "rxjs/Subscription";
import { Observable } from "rxjs/Observable";

import { Entry, User, EntryCommentDto } from "./../dtos";
import { EntriesService } from "./entries.service";
import { NotificationService } from "./../common/notification.service";
import { UserService } from "../user/user.service";

@Component({
  selector: "comments",
  templateUrl: "html/entries/comments.component.html"
})
export class CommentsComponent implements OnInit, OnDestroy {
  @Input()
  entry: Entry;

  newComment: string;
  signedInUser: User;

  private signedInUser$: Subscription;
  
  constructor(
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private entriesService: EntriesService,
    private notificationService: NotificationService) { }

  ngOnInit() {
    this.signedInUser$ = this.userService.signedInUser.subscribe(user => this.signedInUser = user);
  }

  onCommentClick() {
    let comment = new EntryCommentDto();
    comment.entryId = this.entry.id;
    comment.createdAt = new Date();
    comment.author = this.signedInUser;
    comment.comment = this.newComment;
    this.entriesService.comment(comment)
      .subscribe((newComment: EntryCommentDto) => {
        this.entry.comments.push(newComment);
        this.newComment = undefined;
      },
      (error: any) => this.notificationService.addDanger(`There was an error trying to create the comment, please try again.
      ${error}`));
  }

  isMyComment(comment: EntryCommentDto){
    return comment.author.id == this.signedInUser.id;
  }

  ngOnDestroy() {
	  this.signedInUser$.unsubscribe();
  }
}

@Pipe({ name: 'orderByCommentDate' })
export class OrderByCommentDatePipe implements PipeTransform {
  transform(comments: EntryCommentDto[]) {
    return comments.sort((e1, e2) => e1.createdAt.getTime() - e2.createdAt.getTime());
  }
}