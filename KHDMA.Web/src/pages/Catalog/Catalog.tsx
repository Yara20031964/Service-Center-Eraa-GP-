import { useCallback, useEffect, useState } from 'react'
import type { TokenResponse } from '../../api/auth'
import { UnauthorizedError } from '../../api/admin'
import {
  addServiceImages,
  assetUrl,
  createCategory,
  createService,
  deleteCategory,
  deleteService,
  deleteServiceImage,
  getCategories,
  getServiceImages,
  getServicesByCategory,
  toggleCategoryActive,
  updateService,
  type Category,
  type Service,
  type ServiceImage,
  type ServiceInput,
} from '../../api/catalog'
import ConfirmDialog from '../../components/ConfirmDialog'
import Toggle from '../../components/Toggle'
import { PencilIcon, PlusIcon, Spinner, TrashIcon } from '../../components/icons'
import './Catalog.css'

type Selection =
  | { kind: 'service'; service: Service }
  | { kind: 'new'; categoryId: string }
  | null

export default function Catalog({
  session,
  onLogout,
}: {
  session: TokenResponse
  onLogout: () => void
}) {
  const token = session.accessToken

  const [categories, setCategories] = useState<Category[]>([])
  const [loadingCats, setLoadingCats] = useState(true)
  const [catsError, setCatsError] = useState<string | null>(null)

  const [expanded, setExpanded] = useState<Set<string>>(new Set())
  const [services, setServices] = useState<Record<string, Service[]>>({})
  const [loadingSvc, setLoadingSvc] = useState<Set<string>>(new Set())

  const [selection, setSelection] = useState<Selection>(null)

  // Add-category inline form
  const [showAddCat, setShowAddCat] = useState(false)
  const [catEn, setCatEn] = useState('')
  const [catAr, setCatAr] = useState('')
  const [catBusy, setCatBusy] = useState(false)

  // Delete-category confirmation
  const [catToDelete, setCatToDelete] = useState<Category | null>(null)
  const [delBusy, setDelBusy] = useState(false)
  const [delError, setDelError] = useState<string | null>(null)

  const guard = useCallback(
    (err: unknown, set: (m: string) => void) => {
      if (err instanceof UnauthorizedError) onLogout()
      else set(err instanceof Error ? err.message : 'Something went wrong.')
    },
    [onLogout],
  )

  const loadCategories = useCallback(async () => {
    setLoadingCats(true)
    setCatsError(null)
    try {
      setCategories(await getCategories(token))
    } catch (err) {
      guard(err, setCatsError)
    } finally {
      setLoadingCats(false)
    }
  }, [token, guard])

  useEffect(() => {
    void loadCategories()
  }, [loadCategories])

  const loadServices = useCallback(
    async (categoryId: string) => {
      setLoadingSvc((s) => new Set(s).add(categoryId))
      try {
        const list = await getServicesByCategory(token, categoryId)
        setServices((m) => ({ ...m, [categoryId]: list }))
      } catch (err) {
        guard(err, () => {})
      } finally {
        setLoadingSvc((s) => {
          const n = new Set(s)
          n.delete(categoryId)
          return n
        })
      }
    },
    [token, guard],
  )

  function toggleExpand(categoryId: string) {
    setExpanded((s) => {
      const n = new Set(s)
      if (n.has(categoryId)) n.delete(categoryId)
      else {
        n.add(categoryId)
        if (!services[categoryId]) void loadServices(categoryId)
      }
      return n
    })
  }

  async function onToggleCategory(cat: Category) {
    setCategories((cs) =>
      cs.map((c) => (c.id === cat.id ? { ...c, isActive: !c.isActive } : c)),
    )
    try {
      await toggleCategoryActive(token, cat.id)
    } catch (err) {
      guard(err, () => {})
      void loadCategories()
    }
  }

  function askDeleteCategory(cat: Category) {
    setDelError(null)
    setCatToDelete(cat)
  }

  async function confirmDeleteCategory() {
    if (!catToDelete) return
    setDelBusy(true)
    setDelError(null)
    try {
      await deleteCategory(token, catToDelete.id)
      if (
        (selection?.kind === 'new' && selection.categoryId === catToDelete.id) ||
        (selection?.kind === 'service' &&
          selection.service.categoryId === catToDelete.id)
      )
        setSelection(null)
      await loadCategories()
      setCatToDelete(null)
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setDelError(err instanceof Error ? err.message : 'Delete failed.')
    } finally {
      setDelBusy(false)
    }
  }

  async function onAddCategory() {
    if (!catEn.trim() || !catAr.trim()) return
    setCatBusy(true)
    try {
      await createCategory(token, { nameEn: catEn.trim(), nameAr: catAr.trim() })
      setCatEn('')
      setCatAr('')
      setShowAddCat(false)
      await loadCategories()
    } catch (err) {
      guard(err, () => {})
    } finally {
      setCatBusy(false)
    }
  }

  return (
    <div className="catalog">
      {/* -------- Left: categories + services -------- */}
      <aside className="cat-panel">
        <div className="cat-panel__head">
          <h1>Categories</h1>
          <button
            type="button"
            className="cat-add"
            onClick={() => setShowAddCat((v) => !v)}
            aria-label="Add category"
          >
            <PlusIcon size={18} />
          </button>
        </div>

        {showAddCat && (
          <div className="cat-new">
            <input
              placeholder="Name (EN)"
              value={catEn}
              onChange={(e) => setCatEn(e.target.value)}
            />
            <input
              placeholder="الاسم (AR)"
              dir="rtl"
              value={catAr}
              onChange={(e) => setCatAr(e.target.value)}
            />
            <div className="cat-new__actions">
              <button
                type="button"
                className="btn btn--sm"
                disabled={catBusy || !catEn.trim() || !catAr.trim()}
                onClick={() => void onAddCategory()}
              >
                {catBusy ? <Spinner /> : 'Add'}
              </button>
              <button
                type="button"
                className="linkbtn"
                onClick={() => setShowAddCat(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        {catsError ? (
          <div className="panelbox">
            <p>{catsError}</p>
            <button className="btn btn--sm" onClick={() => void loadCategories()}>
              Retry
            </button>
          </div>
        ) : loadingCats ? (
          <div className="cat-loading">
            <Spinner /> Loading…
          </div>
        ) : (
          <div className="cat-list">
            {categories.map((cat) => (
              <div key={cat.id} className="cat">
                <div className="cat__row">
                  <button
                    type="button"
                    className={`cat__name${expanded.has(cat.id) ? ' is-open' : ''}`}
                    onClick={() => toggleExpand(cat.id)}
                  >
                    <span className="cat__chev">▸</span>
                    {cat.nameEn}
                  </button>
                  <div className="cat__actions">
                    <Toggle
                      checked={cat.isActive}
                      onChange={() => void onToggleCategory(cat)}
                      label={`Toggle ${cat.nameEn}`}
                    />
                    <button
                      type="button"
                      className="iconlink iconlink--danger"
                      onClick={() => askDeleteCategory(cat)}
                      aria-label={`Delete ${cat.nameEn}`}
                      title="Delete category"
                    >
                      <TrashIcon size={16} />
                    </button>
                  </div>
                </div>

                {expanded.has(cat.id) && (
                  <div className="svc-list">
                    {loadingSvc.has(cat.id) && (
                      <div className="svc-loading">
                        <Spinner /> Loading…
                      </div>
                    )}
                    {(services[cat.id] ?? []).map((svc) => {
                      const active =
                        selection?.kind === 'service' && selection.service.id === svc.id
                      return (
                        <button
                          key={svc.id}
                          type="button"
                          className={`svc${active ? ' svc--active' : ''}${
                            svc.isActive ? '' : ' svc--off'
                          }`}
                          onClick={() => setSelection({ kind: 'service', service: svc })}
                        >
                          {svc.nameEn}
                          {active && <span className="svc__editing">Editing</span>}
                        </button>
                      )
                    })}
                    {!loadingSvc.has(cat.id) &&
                      (services[cat.id]?.length ?? 0) === 0 && (
                        <p className="svc-empty">No services yet.</p>
                      )}
                    <button
                      type="button"
                      className="svc-add"
                      onClick={() => setSelection({ kind: 'new', categoryId: cat.id })}
                    >
                      <PlusIcon size={15} /> Add service
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </aside>

      {/* -------- Right: service editor -------- */}
      <ServiceEditor
        key={
          selection?.kind === 'service'
            ? selection.service.id
            : selection?.kind === 'new'
              ? `new-${selection.categoryId}`
              : 'empty'
        }
        token={token}
        selection={selection}
        onLogout={onLogout}
        onSaved={async (categoryId, selectId) => {
          await loadServices(categoryId)
          setExpanded((s) => new Set(s).add(categoryId))
          if (selectId) {
            const list = await getServicesByCategory(token, categoryId)
            setServices((m) => ({ ...m, [categoryId]: list }))
            const found = list.find((x) => x.id === selectId)
            setSelection(found ? { kind: 'service', service: found } : null)
          }
        }}
      />

      <ConfirmDialog
        open={!!catToDelete}
        title="Delete category"
        message={`Delete “${catToDelete?.nameEn}”? This can't be undone.`}
        busy={delBusy}
        error={delError}
        onConfirm={() => void confirmDeleteCategory()}
        onCancel={() => {
          setCatToDelete(null)
          setDelError(null)
        }}
      />
    </div>
  )
}

/* ================================================================== */
/* Service editor (right panel)                                        */
/* ================================================================== */

function ServiceEditor({
  token,
  selection,
  onLogout,
  onSaved,
}: {
  token: string
  selection: Selection
  onLogout: () => void
  onSaved: (categoryId: string, selectId?: string) => Promise<void>
}) {
  const isNew = selection?.kind === 'new'
  const service = selection?.kind === 'service' ? selection.service : null
  const categoryId =
    selection?.kind === 'new' ? selection.categoryId : service?.categoryId

  const [nameEn, setNameEn] = useState(service?.nameEn ?? '')
  const [nameAr, setNameAr] = useState(service?.nameAr ?? '')
  const [description, setDescription] = useState(service?.description ?? '')
  const [duration, setDuration] = useState(
    service?.estimatedDurationMin != null ? String(service.estimatedDurationMin) : '',
  )
  const [isActive, setIsActive] = useState(service?.isActive ?? true)
  const [newImages, setNewImages] = useState<File[]>([])
  const [existingImages, setExistingImages] = useState<ServiceImage[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [delBusy, setDelBusy] = useState(false)
  const [delError, setDelError] = useState<string | null>(null)

  useEffect(() => {
    if (service) getServiceImages(token, service.id).then(setExistingImages).catch(() => {})
  }, [token, service])

  if (!selection) {
    return (
      <section className="editor editor--empty">
        <div className="editor__placeholder">
          <span className="editor__placeholder-icon">
            <PencilIcon size={26} />
          </span>
          <p>Select a service to edit, or add a new one.</p>
        </div>
      </section>
    )
  }

  async function onSave() {
    if (!nameEn.trim() || !nameAr.trim()) {
      setError('English and Arabic names are required.')
      return
    }
    if (!categoryId) return
    setBusy(true)
    setError(null)
    const input: ServiceInput = {
      nameEn: nameEn.trim(),
      nameAr: nameAr.trim(),
      description: description.trim(),
      estimatedDurationMin: duration ? Number(duration) : null,
      isActive,
    }
    try {
      if (isNew) {
        await createService(token, categoryId, input, newImages)
        await onSaved(categoryId)
      } else if (service) {
        await updateService(token, service.id, input)
        if (newImages.length) await addServiceImages(token, service.id, newImages)
        await onSaved(categoryId, service.id)
      }
      setNewImages([])
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setBusy(false)
    }
  }

  async function runDelete() {
    if (!service) return
    setDelBusy(true)
    setDelError(null)
    try {
      await deleteService(token, service.id)
      await onSaved(service.categoryId, service.id) // reloads; selection clears
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setDelError(err instanceof Error ? err.message : 'Delete failed.')
    } finally {
      setDelBusy(false)
    }
  }

  async function removeExistingImage(imageId: string) {
    try {
      await deleteServiceImage(token, imageId)
      setExistingImages((imgs) => imgs.filter((x) => x.id !== imageId))
    } catch (err) {
      if (err instanceof UnauthorizedError) onLogout()
      else setError(err instanceof Error ? err.message : 'Could not remove image.')
    }
  }

  return (
    <section className="editor">
      <div className="editor__head">
        <h2>{isNew ? 'New Service' : `Edit: ${service?.nameEn}`}</h2>
        <label className="editor__active">
          <span>Service Active</span>
          <Toggle checked={isActive} onChange={setIsActive} label="Service active" />
        </label>
      </div>

      <div className="editor__body">
        <div className="grid-2">
          <div className="field">
            <label>Service Name (EN)</label>
            <input value={nameEn} onChange={(e) => setNameEn(e.target.value)} />
          </div>
          <div className="field">
            <label>Service Name (AR)</label>
            <input
              dir="rtl"
              value={nameAr}
              onChange={(e) => setNameAr(e.target.value)}
            />
          </div>
        </div>

        <div className="field">
          <label>Description</label>
          <textarea
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="field field--narrow">
          <label>Estimated Duration (mins)</label>
          <input
            type="number"
            min={0}
            value={duration}
            onChange={(e) => setDuration(e.target.value)}
          />
        </div>

        <div className="field">
          <label>Service Images</label>
          <div className="images">
            {existingImages.map((img) => (
              <div key={img.id} className="imgtile">
                <img src={assetUrl(img.imageUrl)} alt="" />
                <button
                  type="button"
                  className="imgtile__remove"
                  onClick={() => void removeExistingImage(img.id)}
                  aria-label="Delete image"
                >
                  ✕
                </button>
              </div>
            ))}
            {newImages.map((file, i) => (
              <div key={`n${i}`} className="imgtile imgtile--new">
                <img src={URL.createObjectURL(file)} alt="" />
                <button
                  type="button"
                  className="imgtile__remove"
                  onClick={() =>
                    setNewImages((imgs) => imgs.filter((_, idx) => idx !== i))
                  }
                  aria-label="Remove image"
                >
                  ✕
                </button>
              </div>
            ))}
            <label className="imgtile imgtile--add">
              <PlusIcon size={20} />
              <input
                type="file"
                accept="image/*"
                multiple
                hidden
                onChange={(e) => {
                  const files = Array.from(e.target.files ?? [])
                  setNewImages((imgs) => [...imgs, ...files])
                  e.target.value = ''
                }}
              />
            </label>
          </div>
        </div>

        {error && <div className="inline-error">{error}</div>}
      </div>

      <div className="editor__foot">
        {!isNew && (
          <button
            type="button"
            className="linkbtn linkbtn--danger"
            onClick={() => {
              setDelError(null)
              setConfirmDelete(true)
            }}
            disabled={busy}
          >
            Delete Service
          </button>
        )}
        <button type="button" className="btn editor__save" onClick={() => void onSave()} disabled={busy}>
          {busy ? <Spinner /> : isNew ? 'Create Service' : 'Save Changes'}
        </button>
      </div>

      <ConfirmDialog
        open={confirmDelete}
        title="Delete service"
        message={`Delete “${service?.nameEn}”? This can't be undone.`}
        busy={delBusy}
        error={delError}
        onConfirm={() => void runDelete()}
        onCancel={() => {
          setConfirmDelete(false)
          setDelError(null)
        }}
      />
    </section>
  )
}
