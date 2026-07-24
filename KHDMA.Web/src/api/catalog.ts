// Catalog (Categories + Services) admin API client.
import { UnauthorizedError } from './admin'

/** API host for static assets (image URLs are returned as /uploads/... paths). */
const ASSET_ORIGIN = 'https://khdma.runasp.net'

export function assetUrl(url: string): string {
  if (!url) return url
  return /^https?:\/\//.test(url) ? url : `${ASSET_ORIGIN}${url}`
}

/* ---------- Types ---------- */

export interface Category {
  id: string
  nameEn: string
  nameAr: string
  description: string | null
  iconUrl: string | null
  isActive: boolean
}

export interface Service {
  id: string
  categoryId: string
  nameEn: string
  nameAr: string
  description: string | null
  image: string | null
  fixedPrice: number | null
  estimatedDurationMin: number | null
  estimatedDurationMax: number | null
  rating: number
  reviewCount: number
  isActive: boolean
  imageUrls: string[]
}

export interface ServiceImage {
  id: string
  imageUrl: string
}

interface Wrapped<T> {
  success: boolean
  message?: string
  data: T
}

/* ---------- Low-level helpers ---------- */

// Reads the response, throwing UnauthorizedError on 401/403 and the API's
// `message` (e.g. the 409 "has bookings" text) on other failures.
async function parse<T>(res: Response): Promise<T> {
  if (res.status === 401 || res.status === 403) throw new UnauthorizedError()
  const text = await res.text()
  const body = text ? JSON.parse(text) : {}
  if (!res.ok) {
    throw new Error(body?.message || `Request failed (${res.status}).`)
  }
  return body as T
}

async function reqJson<T>(
  method: string,
  path: string,
  token: string,
  body?: unknown,
): Promise<T> {
  let res: Response
  try {
    res = await fetch(path, {
      method,
      headers: {
        Authorization: `Bearer ${token}`,
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  return parse<T>(res)
}

async function reqForm(
  method: string,
  path: string,
  token: string,
  form: FormData,
): Promise<void> {
  let res: Response
  try {
    res = await fetch(path, {
      method,
      headers: { Authorization: `Bearer ${token}` }, // browser sets multipart boundary
      body: form,
    })
  } catch {
    throw new Error('Cannot reach the server. Check your connection and try again.')
  }
  await parse<unknown>(res)
}

/* ---------- Categories ---------- */

export async function getCategories(token: string): Promise<Category[]> {
  const r = await reqJson<Wrapped<Category[]>>(
    'GET',
    '/api/admin/categories?page=1&pageSize=200',
    token,
  )
  return r.data ?? []
}

export function createCategory(
  token: string,
  body: { nameEn: string; nameAr: string; description?: string },
): Promise<unknown> {
  return reqJson('POST', '/api/admin/categories', token, { ...body, isActive: true })
}

export function updateCategory(
  token: string,
  id: string,
  body: { nameEn?: string; nameAr?: string; description?: string },
): Promise<unknown> {
  return reqJson('PUT', `/api/admin/categories/${id}`, token, body)
}

export function toggleCategoryActive(token: string, id: string): Promise<unknown> {
  return reqJson('PUT', `/api/admin/categories/${id}/toggle-active`, token)
}

export function deleteCategory(token: string, id: string): Promise<unknown> {
  return reqJson('DELETE', `/api/admin/categories/${id}`, token)
}

/* ---------- Services ---------- */

export async function getServicesByCategory(
  token: string,
  categoryId: string,
): Promise<Service[]> {
  const r = await reqJson<Wrapped<Service[]>>(
    'GET',
    `/api/admin/services?categoryId=${categoryId}&page=1&pageSize=200`,
    token,
  )
  return r.data ?? []
}

export interface ServiceInput {
  nameEn: string
  nameAr: string
  description: string
  estimatedDurationMin: number | null
  isActive: boolean
}

function serviceForm(input: ServiceInput, images?: File[], categoryId?: string): FormData {
  const f = new FormData()
  if (categoryId) f.append('CategoryId', categoryId)
  f.append('NameEn', input.nameEn)
  f.append('NameAr', input.nameAr)
  f.append('Description', input.description)
  if (input.estimatedDurationMin != null)
    f.append('EstimatedDurationMin', String(input.estimatedDurationMin))
  f.append('IsActive', String(input.isActive))
  images?.forEach((file) => f.append('ImageUrls', file))
  return f
}

export function createService(
  token: string,
  categoryId: string,
  input: ServiceInput,
  images: File[],
): Promise<void> {
  return reqForm('POST', '/api/admin/services', token, serviceForm(input, images, categoryId))
}

export function updateService(
  token: string,
  id: string,
  input: ServiceInput,
): Promise<void> {
  return reqForm('PUT', `/api/admin/services/${id}`, token, serviceForm(input))
}

export function toggleServiceActive(token: string, id: string): Promise<unknown> {
  return reqJson('PUT', `/api/admin/services/${id}/toggle-active`, token)
}

export function deleteService(token: string, id: string): Promise<unknown> {
  return reqJson('DELETE', `/api/admin/services/${id}`, token)
}

export async function getServiceImages(
  token: string,
  id: string,
): Promise<ServiceImage[]> {
  const r = await reqJson<Wrapped<ServiceImage[]>>(
    'GET',
    `/api/admin/services/${id}/images`,
    token,
  )
  return r.data ?? []
}

export function addServiceImages(
  token: string,
  id: string,
  images: File[],
): Promise<void> {
  const f = new FormData()
  images.forEach((file) => f.append('images', file))
  return reqForm('POST', `/api/admin/services/${id}/images`, token, f)
}

export function deleteServiceImage(token: string, imageId: string): Promise<unknown> {
  return reqJson('DELETE', `/api/admin/services/images/${imageId}`, token)
}
