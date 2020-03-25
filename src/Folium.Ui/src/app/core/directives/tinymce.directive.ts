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
import {
    Directive,
    OnDestroy,
    AfterViewInit,
    Provider,
    forwardRef,
    HostBinding,
    NgZone
} from "@angular/core";
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from "@angular/forms";
import { DomSanitizer } from "@angular/platform-browser";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { SecurityService } from "../security.service";
import { environment } from '../../../environments/environment';

var tinymce = require('tinymce/tinymce');
require('tinymce/plugins/autoresize');
require('tinymce/plugins/lists');
require('tinymce/plugins/image');
require('tinymce/plugins/link');
require('tinymce/plugins/autolink');
require('tinymce/plugins/code');
require('tinymce/plugins/charmap');
require('tinymce/plugins/fullscreen');
require('tinymce/plugins/imagetools');
require('tinymce/themes/silver/theme');

export const TinyMceValueAccessor: Provider = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TinyMceDirective),
    multi: true
};

@Directive({
    selector: "[tinyMceEditor]",
    providers: [TinyMceValueAccessor]
})

export class TinyMceDirective implements OnDestroy, AfterViewInit, ControlValueAccessor {
    static nextUniqueId = 0;
    @HostBinding("attr.data-tinymce-uniqueid")
    uniqueId: string;

    onTouchedCallback: () => void = () => { };
    onChangeCallback: (_: any) => void = () => { };
    innerValue: any;
    init = false;

    constructor(
      private zone: NgZone,
      private securityService: SecurityService,
      private http: HttpClient,
        private sanitizer: DomSanitizer) {
        this.uniqueId = `tinymce-host-${TinyMceDirective.nextUniqueId++}`;
    }

    get value(): any {
        return this.innerValue;
    };
    set value(v: any) {
        if (v !== this.innerValue) {
            this.innerValue = v;
			this.zone.run(() => {
				this.onChangeCallback(v);
			});
        }
    }

  ngAfterViewInit(): void {
    let self = this;
    tinymce.baseURL = "/lib/tinymce";
    tinymce.suffix = ".min";
    tinymce.init({
      selector: `[data-tinymce-uniqueid=${this.uniqueId}]`,
      schema: "html5",
      skin_url: "/lib/tinymce/skins/ui/oxide",
      plugins: "autoresize, lists, link, autolink, image, code, charmap, fullscreen, imagetools",
      autoresize_bottom_margin: 5,
      min_height: 200,
      max_height: 500,
      menubar: false,
      statusbar: false,
      toolbar: ["undo redo | bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | bullist numlist |",
      " forecolor backcolor removeformat | link image charmap emoticons | fullscreen | code |"],
      setup: ed => {
        ed.on("init", ed2 => {
          if (this.innerValue) ed2.target.setContent(this.innerValue);
          this.init = true;
        });
        ed.on("change keyup", () => {
          const content = ed.getContent();
          this.value = content;
        });
        ed.on("click", (e) => {
          ed.container.click(e); // bubble the click.
        });
      },
      images_upload_url: 'file-uploads/tinymce',
      images_upload_handler: function (blobInfo, success, failure) {
        const formData: FormData = new FormData();
        formData.append('file', blobInfo.blob(), blobInfo.filename() /*fileName(blobInfo)*/);

        self.http.post('file-uploads/tinymce',
          formData,
          {
            headers: new HttpHeaders({
              'Authorization': `Bearer ${self.securityService.authenticationToken}`
            }),
          }).subscribe((response: any) => {
            if (typeof response.location != 'string') {
              failure('Invalid JSON: ' + response);
              return;
            }
            success(
              environment.apiRootUri + response.location);
          });
      },
      image_class_list: [
        { title: 'img-fluid', value: 'img-fluid' }
      ]
    });
    // Update value on blur.
    tinymce.activeEditor.on("blur", () => this.updateValue());
    }

    updateValue() {
        const content = tinymce.activeEditor.getContent();
	    this.value = content; //this.sanitizer.bypassSecurityTrustHtml(content);
    }

    writeValue(value): void {
        if (value !== this.innerValue) {
            this.innerValue = value;
            if (this.init && value) tinymce.activeEditor.setContent(value);
        }
    }

    registerOnChange(fn): void {
        this.onChangeCallback = fn;
    }

    registerOnTouched(fn): void {
        this.onTouchedCallback = fn;
    }

    ngOnDestroy(): void {
        if (this.init) tinymce.remove(`[data-tinymce-uniqueid=${this.uniqueId}]`);
    }
}
