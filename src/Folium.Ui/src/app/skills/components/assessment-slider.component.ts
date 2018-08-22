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

/** Adapted from Material2 slider. https://github.com/angular/material2  **/
import {
  NgModule,
  Component,
  ElementRef,
  HostBinding,
  Input,
  Injectable,
  ViewEncapsulation,
  forwardRef,
  ChangeDetectorRef,
  OnChanges,
  SimpleChanges
} from "@angular/core";
import {
  NG_VALUE_ACCESSOR,
  ControlValueAccessor,
  FormsModule,
} from "@angular/forms";
import {CommonModule} from "@angular/common";
import { HammerGestureConfig } from "@angular/platform-browser";
import { HAMMER_GESTURE_CONFIG } from "@angular/platform-browser";
import { SelfAssessmentScale, SelfAssessment } from "../../core/dtos";

const noop = () => {
};

export const CUSTOM_INPUT_CONTROL_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => AssessmentSliderComponent),
    multi: true
};

@Component({
    host: {
        "tabindex": "0",
        "(click)": "onClick($event)",
        "(slide)": "onSlide($event)",
        "(slidestart)": "onSlideStart($event)",
        "(slideend)": "onSlideEnd($event)",
        "(window:resize)": "onResize()",
        "(blur)": "onBlur()",
    },
    encapsulation: ViewEncapsulation.None,
    providers: [CUSTOM_INPUT_CONTROL_VALUE_ACCESSOR],
    selector: "assessment-slider",
    template: ` 
    <div class="assessment-slider-wrapper" [class.no-assessment-value]="!selfAssessment">
      <div class="assessment-slider-container"
        [class.assessment-slider-sliding]="isSliding"
        [class.assessment-slider-active]="isActive">
        <div class="assessment-slider-track-container">
          <div class="assessment-slider-track"></div>
          <div class="assessment-slider-track assessment-slider-track-fill" [style.border-color]="filledSliderTrackColour"></div>
        </div>
        <div class="assessment-slider-thumb-container">
          <div class="assessment-slider-thumb-position">
            <div class="assessment-slider-thumb" [style.background-color]="sliderThumbColour"></div>
          </div>
        </div>  
      </div>
      <div>              
        <ul class="assessment-slider-ticks">
          <li *ngFor="let tick of scale" [style.visibility]="isTickVisible(tick) ? 'visible' : 'hidden'" [style.background-color]="getSelfAssessmentScaleColour(tick)" class="assessment-slider-tick" ></li>
        </ul>
      </div>                             
    </div>`
})
export class AssessmentSliderComponent implements ControlValueAccessor, OnChanges {
  @Input()
  @BooleanFieldValue()
  @HostBinding("class.assessment-slider-disabled")
  @HostBinding("attr.aria-disabled")
  disabled: boolean = false;

  /** The self assessment value used for the ngModel value for the slider. */
  selfAssessment: SelfAssessment;

  /** The filled colour track. */
  filledSliderTrackColour: string = "";

  /** The slider thumb colour. */
  sliderThumbColour: string = "";

  /**
   * Whether or not the thumb is sliding.
   * Used to determine if there should be a transition for the thumb and fill track.
   * TODO: internal
   */
  isSliding: boolean = false;

  /**
   * Whether or not the slider is active (clicked or sliding).
   * Used to shrink and grow the thumb as according to the Material Design spec.
   * TODO: internal
   */
  isActive: boolean = false;

  /** A renderer to handle updating the slider"s thumb and fill track. */
  private renderer: SliderRenderer = undefined;

  /** The dimensions of the slider. */
  private sliderDimensions: ClientRect = undefined;

  /** The maximum value that the slider can have. */
  private _max: number = 100;

  /** The percentage of the slider that coincides with the value. */
  private percent: number = 0;

  /** The assessment scale used to plot on the slider. */
  private _scale: SelfAssessmentScale[] = [];

  /** Value of the slider. */
  private _value: number = 0;

  /** Value of the slider before any sliding, this will not be updated as often as the value, so can be used to raise meaningful change event. */
  private _valueOnSlide: number;

  private wasVisible: boolean;

  // Placeholders for the callbacks which are later providesd
  // by the Control Value Accessor
  private onTouchedCallback: () => void = noop;
  private onChangeCallback: (_: SelfAssessment) => void = noop;

  @Input()
  skillId: number;

  @Input()
  visible: boolean;

  @HostBinding("attr.aria-valuemin")
  get min() {
    return 0;
  }

  @HostBinding("attr.aria-valuemax")
  get max() {
    return this._max;
  }
  set max(v: number) {
    this._max = Number(v);
  }

  @HostBinding("attr.aria-valuenow")
  get value() {
    return this._value;
  }
  set value(v: number) {
    this._value = Number(v);
	if (this.scale.length > 0) {
		this.selfAssessment = new SelfAssessment(this.scale[v].levelId, this.skillId, this.scale[v].score, new Date(Date.now()));
		// Raise the event if we are not sliding and the value has changed.
		if (!this.isSliding && (this._value !== this._valueOnSlide)) {
			this.onChangeCallback(this.selfAssessment);
		}
		// Update the slide value if we are no longer sliding.
		this._valueOnSlide = this.isSliding ? this._valueOnSlide : Number(v);
	}
  }

  @Input()
  get scale() {
    return this._scale;
  }
  set scale(v: SelfAssessmentScale[]) {
    this._scale = v;
    this.max = v.length - 1;
  }

  constructor(elementRef: ElementRef, private changeDetectorRef: ChangeDetectorRef) {
    this.renderer = new SliderRenderer(elementRef);
  }

  ngOnChanges(changes: SimpleChanges) {
    let visible = changes["visible"];
    if (visible &&
    visible.currentValue === true &&
    visible.currentValue !== visible.previousValue) {
      // Trigger a resize if we have just been shown.
      this.onResize();
    }
  }

  // From ControlValueAccessor interface
  writeValue(v: SelfAssessment) {
    this.selfAssessment = v;
      if (!v) {
      // If v is not defined just set it to the min value.
      v = new SelfAssessment(this.scale[this.min].levelId, this.skillId, this.scale[this.min].score, new Date(Date.now()));
    }
    let newValue = this._scale.findIndex(s => s.score === v.score);
    if (newValue !== this._value) {
      this.isActive = true;
      this.isSliding = false;
      //this.renderer.addFocus();
      this._value = newValue;
      this._valueOnSlide = newValue;
      this.snapToThumbValue();
    }

    this.updateSliderAndThumbColourFromValue();
    this.changeDetectorRef.markForCheck();
  }

  // From ControlValueAccessor interface
  registerOnChange(fn: any) {
      this.onChangeCallback = fn;
  }

  // From ControlValueAccessor interface
  registerOnTouched(fn: any) {
      this.onTouchedCallback = fn;
  }

  /** TODO: internal */
  onClick(event: MouseEvent) {
    if (this.disabled) {
      return;
    }
    this.isActive = true;
    this.isSliding = false;
    this.renderer.addFocus();
    this.updateValueFromPosition(event.clientX);
    this.snapToThumbValue();
    this.updateSliderAndThumbColourFromValue();
  }

  onSlide(event: HammerInput) {    
    // Do nothing if disabled or not sliding in the correct direction.
    if (this.disabled || !(event.direction == Hammer.DIRECTION_LEFT || event.direction == Hammer.DIRECTION_RIGHT)) {
      return;
    }

    // Prevent the slide from selecting anything else.
	  event.preventDefault();
	  this.isSliding = true;
    this.updateValueFromPosition(event.center.x);
    this.updateSliderAndThumbColourFromValue();
  }

  onSlideStart(event: HammerInput) {
    // Do nothing if disabled or not sliding in the correct direction.
    if (this.disabled || !(event.direction == Hammer.DIRECTION_LEFT || event.direction == Hammer.DIRECTION_RIGHT)) {
      return;
    }

    event.preventDefault();
    this.isSliding = true;
    this.isActive = true;
    this.renderer.addFocus();
    this.updateValueFromPosition(event.center.x);
  }

  onSlideEnd(event: HammerInput) {
    // Do nothing if disabled or not sliding in the correct direction.
    if (this.disabled || !(event.direction == Hammer.DIRECTION_LEFT || event.direction == Hammer.DIRECTION_RIGHT)) {
      return;
    }

    this.isSliding = false;
    // write the value again, now we have finished sliding.
    this.value = this._value;
    this.snapToThumbValue();
  }

  onResize() {
    if (this.visible === false) return; // only resize if the slider is visible.
    let sliderDimensions = this.renderer.getSliderDimensions();
    if (sliderDimensions.width > 0) {
      // Only update if we have a width.
      this.sliderDimensions = sliderDimensions;
      // Skip updating the value and position as there is no new placement.
      this.renderer.updateThumbAndFillPosition(this.percent, this.sliderDimensions.width);
    }
  }

  onBlur() {
    this.isActive = false;
    this.onTouchedCallback();
  }

  /**
   * Return whether a tick should be visible.
   */
  isTickVisible(tick: SelfAssessmentScale) {
	return this.isSliding || !this.selfAssessment || tick.score >= this.selfAssessment.score;
  }

  getSelfAssessmentScaleColour(selfAssessmentScale: SelfAssessmentScale) {
    return AssessmentSliderComponent.getScaleColour(selfAssessmentScale);
  }

  static getScaleColour(selfAssessmentScale: SelfAssessmentScale) {
    return "hsl(" + selfAssessmentScale.score + ",100%,40%)";
  }

  /**
   * When the value changes without a physical position, the percentage needs to be recalculated
   * independent of the physical location.
   * This is also used to move the thumb to a snapped value once sliding is done.
   */
  private updatePercentFromValue() {
    this.percent = this.value / this.max;
  }

  /**
   * When the value changes update the colour of the the thumb handle and filled slider track.
   */
  private updateSliderAndThumbColourFromValue() {
    let scale = this.scale[this.value ? this.value : 0];
    if (scale) {
      this.sliderThumbColour = this.getSelfAssessmentScaleColour(scale);
      this.filledSliderTrackColour = this.sliderThumbColour;
    }
  }

  /**
   * Calculate the new value from the new physical location. The value will always be snapped.
   */
  private updateValueFromPosition(pos: number) {
    if (!this.sliderDimensions) {
      this.sliderDimensions = this.renderer.getSliderDimensions();
    }
    let offset = this.sliderDimensions.left;
    let size = this.sliderDimensions.width;

    // The exact value is calculated from the event and used to find the closest snap value.
    this.percent = this.clamp((pos - offset) / size);
    let exactValue = this.percent * this.max;

    // This calculation finds the closest step by finding the closest whole number.
    let closestValue = Math.round(exactValue - this.min);
    // The value needs to snap to the min and max.
    this.value = this.clamp(closestValue, this.min, this.max);
    this.renderer.updateThumbAndFillPosition(this.percent, this.sliderDimensions.width);
  }

  /**
   * Snaps the thumb to the current value.
   * Called after a click or drag event is over.
   */
  private snapToThumbValue() {
    if (!this.sliderDimensions) {
      this.sliderDimensions = this.renderer.getSliderDimensions();
    }
    this.updatePercentFromValue();
    if (this.visible === false) return;
    this.renderer.updateThumbAndFillPosition(this.percent, this.sliderDimensions.width);
  }

  /**
   * Return a number between two numbers.
   */
  private clamp(value: number, min = 0, max = 1) {
    return Math.max(min, Math.min(value, max));
  }
}

/**
 * Renderer class in order to keep all dom manipulation in one place and outside of the main class.
 */
class SliderRenderer {
  private sliderElement: HTMLElement;

  constructor(elementRef: ElementRef) {
    this.sliderElement = elementRef.nativeElement;
  }

  /**
   * Get the bounding client rect of the slider track element.
   * The track is used rather than the native element to ignore the extra space that the thumb can
   * take up.
   */
  getSliderDimensions() {
    let trackElement = this.sliderElement.querySelector(".assessment-slider-track");
    return trackElement.getBoundingClientRect();
  }

  /**
   * Update the physical position of the thumb and fill track on the slider.
   */
  updateThumbAndFillPosition(percent: number, width: number) {
    // A container element that is used to avoid overwriting the transform on the thumb itself.
    let thumbPositionElement =
        <HTMLElement>this.sliderElement.querySelector(".assessment-slider-thumb-position");
    let fillTrackElement = <HTMLElement>this.sliderElement.querySelector(".assessment-slider-track-fill");

    let position = percent * width;

    fillTrackElement.style.width = `${percent * 100}%`;
    applyCssTransform(thumbPositionElement, `translateX(${position}px)`);
  }

  /**
   * Focuses the native element.
   * Currently only used to allow a blur event to fire but will be used with keyboard input later.
   */
  addFocus() {
    this.sliderElement.focus();
  }
}

/**
 * Applies a CSS transform to an element, including browser-prefixed properties.
 * @param element
 * @param transformValue
 */
function applyCssTransform(element: HTMLElement, transformValue: string) {
  // It"s important to trim the result, because the browser will ignore the set operation
  // if the string contains only whitespace.
  let value = transformValue.trim();

  element.style.transform = value;
  element.style.webkitTransform = value;
}

/**
 * Annotation Factory that allows HTML style boolean attributes. For example,
 * a field declared like this:
 * @Directive({ selector: "component" }) class MyComponent {
 *   @Input() @BooleanFieldValueFactory() myField: boolean;
 * }
 *
 * You could set it up this way:
 *   <component myField>
 * or:
 *   <component myField="">
 */
export function BooleanFieldValue() {
  return function booleanFieldValueMetadata(target: any, key: string): void {
    const defaultValue = target[key];
    const localKey = `__md_private_symbol_${key}`;
    target[localKey] = defaultValue;

    Object.defineProperty(target, key, {
      get() { return (<any>this)[localKey]; },
      set(value: boolean) {
        (<any>this)[localKey] = value != null && `${value}` !== "false";
      }
    });
  };
}

/* Adjusts configuration of our gesture library, Hammer. */
@Injectable()
export class MatGestureConfig extends HammerGestureConfig {

  /* List of new event names to add to the gesture support list */
  events: string[] = [
    "longpress",
    "slide",
    "slidestart",
    "slideend",
    "slideright",
    "slideleft"
  ];

  /*
   * Builds Hammer instance manually to add custom recognizers that match the Material Design spec.
   *
   * Our gesture names come from the Material Design gestures spec:
   * https://www.google.com/design/spec/patterns/gestures.html#gestures-touch-mechanics
   *
   * More information on default recognizers can be found in Hammer docs:
   * http://hammerjs.github.io/recognizer-pan/
   * http://hammerjs.github.io/recognizer-press/
   *
   * TODO: Confirm threshold numbers with Material Design UX Team
   * */
  buildHammer(element: HTMLElement) {
    const mc = new Hammer(element, {
            touchAction: "pan-y",
        }); 
        // This is an attempt to allow vertical scrolling, as it is disabled on the Hammer elements otherwise.
        // See https://stackoverflow.com/a/42425301

    // Default Hammer Recognizers.
    let pan = new Hammer.Pan();
    let swipe = new Hammer.Swipe();
    let press = new Hammer.Press();

    // Notice that a HammerJS recognizer can only depend on one other recognizer once.
    // Otherwise the previous `recognizeWith` will be dropped.
    let slide = this.createRecognizer(pan, {event: "slide", threshold: 0}, swipe);
    let longpress = this.createRecognizer(press, {event: "longpress", time: 500});

    // Overwrite the default `pan` event to use the swipe event.
    pan.recognizeWith(swipe);

    // Add customized gestures to Hammer manager
    mc.add([swipe, press, pan, slide, longpress]);

    return mc;
  }

  /** Creates a new recognizer, without affecting the default recognizers of HammerJS */
  private createRecognizer(base: Recognizer, options: any, ...inheritances: Recognizer[]) {
    let recognizer = new (<RecognizerStatic> base.constructor)(options);

    inheritances.push(base);
    inheritances.forEach((item) => recognizer.recognizeWith(item));

    return recognizer;
  }
}


@NgModule({
  imports: [CommonModule, FormsModule],
  exports: [AssessmentSliderComponent],
  declarations: [AssessmentSliderComponent],
  providers: [
    {provide: HAMMER_GESTURE_CONFIG, useClass: MatGestureConfig},
  ],
})
export class AssessmentSliderModule { }