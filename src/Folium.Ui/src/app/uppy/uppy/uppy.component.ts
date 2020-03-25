import { Component, OnInit, AfterViewInit, ViewEncapsulation, Input, ChangeDetectionStrategy } from '@angular/core';
import { Subject } from 'rxjs'
import * as Uppy from '@uppy/core';
import { UppyService } from '../uppy.service';
import { v4 } from 'uuid'

console.dir(Uppy)
/*export enum UppyPlugins {
  Tus,
  GoogleDrive,
  Dropbox,
  Instagram,
  Webcam
}*/

export type UppyPluginConfigurations = [
  string, // We need to identity the plugin by name as we are unable to access this once this is minified.
  Uppy.Plugin,
    any
][]

@Component({
    selector: 'uppy',
    templateUrl: './uppy.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UppyComponent implements OnInit, AfterViewInit {
    @Input() config: any = {}
    @Input() plugins: UppyPluginConfigurations = []
    @Input() on: Subject<[string, any, any, any]>

    uuid = v4()

    constructor(public uppyService: UppyService) {
    }

    ngOnInit() {
    }

    ngAfterViewInit() {
        const uppyInstance = this.uppyService.configure(this.config, this.plugins, this.uuid)

        const events = ['file-added', 'file-removed', 'upload', 'upload-progress', 'upload-success', 'complete', 'upload-error', 'info-visible', 'info-hidden']

        events.forEach(ev => uppyInstance.on(ev, (data1, data2, data3) => {
            if (this.on)
                this.on.next([ev, data1, data2, data3])

        }))
    }
}
