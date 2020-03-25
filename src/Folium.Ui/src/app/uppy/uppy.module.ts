import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UppyComponent } from './uppy/uppy.component';
import { UppyService } from './uppy.service';

@NgModule({
    imports: [
        CommonModule
    ],
    declarations: [UppyComponent],
    exports: [UppyComponent],
    providers: [UppyService]
})
export class UppyModule { }