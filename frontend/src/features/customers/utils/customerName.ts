import type { Customer } from '../types/customer.types'

type CustomerNameSource = Pick<
  Customer,
  'firstName' | 'lastName' | 'secondLastName'
>

export function getCustomerFullName(
  customer: CustomerNameSource,
): string {
  return [
    customer.firstName,
    customer.lastName,
    customer.secondLastName,
  ]
    .filter(
      (value): value is string =>
        Boolean(value?.trim()),
    )
    .map((value) => value.trim())
    .join(' ')
}
