// Enum → { label, tone } maps mirroring the backend Domain.Enums.
// tone matches the .badge--<tone> CSS classes.

export interface StatusInfo {
  label: string
  tone: 'green' | 'amber' | 'blue' | 'red' | 'gray'
}

// BookingStatus
export function bookingStatus(n: number): StatusInfo {
  return (
    {
      0: { label: 'Pending', tone: 'amber' },
      1: { label: 'Dispatching', tone: 'blue' },
      2: { label: 'Accepted', tone: 'blue' },
      3: { label: 'En route', tone: 'blue' },
      4: { label: 'Arrived', tone: 'blue' },
      5: { label: 'In Progress', tone: 'blue' },
      6: { label: 'Completed', tone: 'green' },
      7: { label: 'Cancelled', tone: 'red' },
      8: { label: 'No provider', tone: 'red' },
      9: { label: 'Failed', tone: 'red' },
    }[n] ?? { label: '—', tone: 'gray' }
  ) as StatusInfo
}

// UserStatus: Active, Suspended, Banned
export function userStatus(n: number): StatusInfo {
  return (
    {
      0: { label: 'Active', tone: 'green' },
      1: { label: 'Suspended', tone: 'amber' },
      2: { label: 'Banned', tone: 'red' },
    }[n] ?? { label: '—', tone: 'gray' }
  ) as StatusInfo
}

// ProviderState: Pending, Active, Suspended, Banned
export function providerState(n: number): StatusInfo {
  return (
    {
      0: { label: 'Pending', tone: 'amber' },
      1: { label: 'Active', tone: 'green' },
      2: { label: 'Suspended', tone: 'gray' },
      3: { label: 'Banned', tone: 'red' },
    }[n] ?? { label: '—', tone: 'gray' }
  ) as StatusInfo
}

// AvailabilityStatus: Online, Offline, Busy
export function availability(n: number): StatusInfo {
  return (
    {
      0: { label: 'Online', tone: 'green' },
      1: { label: 'Offline', tone: 'gray' },
      2: { label: 'Busy', tone: 'amber' },
    }[n] ?? { label: '—', tone: 'gray' }
  ) as StatusInfo
}

// PaymentStatus: Pending, Paid, Failed, Refunded (serialized as ints)
export function paymentStatus(n: number): StatusInfo {
  return (
    {
      0: { label: 'Pending', tone: 'amber' },
      1: { label: 'Paid', tone: 'green' },
      2: { label: 'Failed', tone: 'red' },
      3: { label: 'Refunded', tone: 'gray' },
    }[n] ?? { label: '—', tone: 'gray' }
  ) as StatusInfo
}

// PayoutStatus arrives as a string label from the API.
export function payoutStatus(s: string): StatusInfo {
  return (
    {
      Requested: { label: 'Requested', tone: 'amber' },
      Pending: { label: 'Pending', tone: 'amber' },
      Approved: { label: 'Approved', tone: 'green' },
      Paid: { label: 'Paid', tone: 'green' },
      Rejected: { label: 'Rejected', tone: 'red' },
    }[s] ?? { label: s || '—', tone: 'gray' }
  ) as StatusInfo
}
