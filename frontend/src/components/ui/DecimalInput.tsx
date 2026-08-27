import type { InputHTMLAttributes, KeyboardEvent } from "react";
export function DecimalInput(props: InputHTMLAttributes<HTMLInputElement>) {
  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowUp" || event.key === "ArrowDown")
      event.preventDefault();
    props.onKeyDown?.(event);
  }
  return (
    <input {...props} type="text" inputMode="decimal" onKeyDown={onKeyDown} />
  );
}
