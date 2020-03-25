/*
 * Slim v4.5.0 - Image Cropping Made Easy
 * Copyright (c) 2017 Rik Schennink - http://slimimagecropper.com
 */
import * as SlimLib from '../../../lib/slim/slim.commonjs.min'; // Copyright (c) SlimLib, http://slimimagecropper.com/ license required.

// Angular core
import { ViewChild, Component, Input, ElementRef } from "@angular/core";

@Component({
  selector: "slim",
  template: "<div #root><ng-content></ng-content></div>"
})

export class Slim {

  @ViewChild("root", { static: true })
  element: ElementRef;

  @Input()
  options: Object;

  ngOnInit(): any {
    SlimLib.create(this.element.nativeElement, this.options);
  }

  ngOnDestroy(): any {
    SlimLib.destroy(this.element.nativeElement);
  }
};