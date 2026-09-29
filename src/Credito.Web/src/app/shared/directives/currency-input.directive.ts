import {
  Directive,
  ElementRef,
  HostListener,
  forwardRef
} from '@angular/core';

import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR
} from '@angular/forms';

@Directive({
  selector: 'input[appCurrency]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CurrencyInputDirective),
      multi: true
    }
  ]
})
export class CurrencyInputDirective
  implements ControlValueAccessor {

  private value = 0;

  private onChange: (value: number) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(
    private readonly elementRef: ElementRef<HTMLInputElement>
  ) {}

  writeValue(value: number | null): void {
    this.value = Number(value ?? 0);
    this.render();
  }

  registerOnChange(
    fn: (value: number) => void
  ): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.elementRef.nativeElement.disabled = disabled;
  }

  @HostListener('input', ['$event'])
  handleInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    const digits = input.value.replace(/\D/g, '');

    this.value = digits
      ? Number(digits) / 100
      : 0;

    this.onChange(this.value);
    this.render();
  }

  @HostListener('blur')
  handleBlur(): void {
    this.onTouched();
    this.render();
  }

  private render(): void {
    this.elementRef.nativeElement.value =
      this.value.toLocaleString(
        'pt-BR',
        {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2
        }
      );
  }
}
