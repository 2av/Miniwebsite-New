import { useEffect, useRef, useState } from 'react'
import Cropper from 'cropperjs'
import 'cropperjs/dist/cropper.css'

type Props = {
  file: File | null
  open: boolean
  onClose: () => void
  onCropped: (blob: Blob, fileName: string) => void
  busy?: boolean
}

/** Matches PHP common/image_upload_crop_modal.php + ImageCropUpload (512×512, 1:1). */
export function ProfileImageCropModal({ file, open, onClose, onCropped, busy }: Props) {
  const imgRef = useRef<HTMLImageElement>(null)
  const cropperRef = useRef<Cropper | null>(null)
  const [objectUrl, setObjectUrl] = useState<string | null>(null)
  const [dims, setDims] = useState('512 × 512 px')
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [zoom, setZoom] = useState(1)

  useEffect(() => {
    if (!open || !file) {
      setObjectUrl(null)
      return
    }
    const url = URL.createObjectURL(file)
    setObjectUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [open, file])

  useEffect(() => {
    if (!open || !objectUrl || !imgRef.current) return

    const img = imgRef.current
    const updatePreview = (cropper: Cropper) => {
      const canvas = cropper.getCroppedCanvas({
        width: 512,
        height: 512,
        imageSmoothingEnabled: true,
        imageSmoothingQuality: 'high',
      })
      if (!canvas) return
      setDims('512 × 512 px')
      setPreviewUrl(canvas.toDataURL('image/png'))
    }

    const cropper = new Cropper(img, {
      aspectRatio: 1,
      viewMode: 1,
      dragMode: 'move',
      autoCropArea: 0.8,
      restore: false,
      guides: true,
      center: true,
      highlight: false,
      cropBoxMovable: true,
      cropBoxResizable: true,
      zoomable: true,
      scalable: true,
      rotatable: true,
      minCropBoxWidth: 100,
      minCropBoxHeight: 100,
      ready() {
        updatePreview(cropper)
      },
      crop() {
        updatePreview(cropper)
      },
      zoom() {
        const data = cropper.getData() as { zoom?: number }
        if (data.zoom != null) setZoom(Math.min(Math.max(data.zoom, 0), 3))
        setTimeout(() => updatePreview(cropper), 50)
      },
    })
    cropperRef.current = cropper

    return () => {
      cropper.destroy()
      cropperRef.current = null
      setPreviewUrl(null)
    }
  }, [open, objectUrl])

  if (!open || !file) return null

  const save = () => {
    const cropper = cropperRef.current
    if (!cropper) return
    const canvas = cropper.getCroppedCanvas({
      width: 512,
      height: 512,
      imageSmoothingEnabled: true,
      imageSmoothingQuality: 'high',
    })
    if (!canvas) return
    canvas.toBlob(
      (blob) => {
        if (!blob) return
        const name = file.name?.replace(/\.\w+$/, '') || 'profile'
        onCropped(blob, `${name}.jpg`)
      },
      'image/jpeg',
      0.92,
    )
  }

  return (
    <div
      className="mw-modal is-open"
      id="imageCropModal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="imageCropModalTitle"
      style={{ display: 'flex', zIndex: 1065 }}
    >
      <div className="mw-modal-backdrop" aria-hidden onClick={busy ? undefined : onClose} />
      <div className="mw-modal-panel mw-modal-lg mw-image-crop-panel" style={{ maxWidth: 'min(56rem, calc(100vw - 2rem))' }}>
        <div className="mw-modal-header">
          <div className="mw-modal-header-main">
            <span className="mw-modal-header-icon" aria-hidden>
              <i className="fa fa-image" />
            </span>
            <div className="mw-modal-header-text-wrap">
              <h2 className="mw-modal-title" id="imageCropModalTitle">
                Adjust &amp; Crop Profile Image
              </h2>
              <p className="mw-modal-subtitle">Drag to adjust. Output is 512×512 for your profile photo.</p>
            </div>
          </div>
          <button type="button" className="mw-modal-close" aria-label="Close" disabled={busy} onClick={onClose}>
            <span aria-hidden>&times;</span>
          </button>
        </div>
        <div className="mw-modal-body" style={{ padding: '1rem 1.25rem' }}>
          <div className="container-fluid px-0">
            <div className="row">
              <div className="col-md-8">
                <div
                  className="crop-container mb-3"
                  style={{
                    width: '100%',
                    height: 380,
                    maxHeight: '50vh',
                    overflow: 'hidden',
                    background: '#000',
                    borderRadius: 8,
                  }}
                >
                  {objectUrl ? (
                    <img ref={imgRef} src={objectUrl} alt="Crop" style={{ maxWidth: '100%', display: 'block' }} />
                  ) : null}
                </div>
                <div className="controls">
                  <div className="row">
                    <div className="col-md-6">
                      <label className="form-label small">Zoom</label>
                      <input
                        type="range"
                        className="form-range"
                        min={0}
                        max={3}
                        step={0.1}
                        value={zoom}
                        onChange={(e) => {
                          const v = parseFloat(e.target.value)
                          setZoom(v)
                          cropperRef.current?.zoomTo(v)
                        }}
                      />
                      <div className="d-flex justify-content-between mb-2 flex-wrap gap-1">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => cropperRef.current?.zoom(-0.1)}
                        >
                          <i className="fa fa-search-minus" /> Zoom Out
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => cropperRef.current?.zoom(0.1)}
                        >
                          <i className="fa fa-search-plus" /> Zoom In
                        </button>
                      </div>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label small">Rotate &amp; Flip</label>
                      <div className="d-flex flex-wrap gap-1">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => cropperRef.current?.rotate(-90)}
                        >
                          <i className="fa fa-rotate-left" />
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => cropperRef.current?.rotate(90)}
                        >
                          <i className="fa fa-rotate-right" />
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => {
                            const d = cropperRef.current?.getImageData()
                            cropperRef.current?.scaleX(-(d?.scaleX || 1))
                          }}
                        >
                          <i className="fa fa-arrows-alt-h" /> H
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => {
                            const d = cropperRef.current?.getImageData()
                            cropperRef.current?.scaleY(-(d?.scaleY || 1))
                          }}
                        >
                          <i className="fa fa-arrows-alt-v" /> V
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => {
                            cropperRef.current?.reset()
                            setZoom(1)
                          }}
                        >
                          <i className="fa fa-refresh" /> Reset
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div className="col-md-4">
                <div className="border rounded p-3 bg-light">
                  <h6 className="mb-2">Preview (512×512)</h6>
                  <div
                    className="crop-preview-box"
                    style={{
                      width: 160,
                      height: 160,
                      overflow: 'hidden',
                      border: '2px solid #e2e8f0',
                      borderRadius: 8,
                      background: '#fff',
                      margin: '0 auto',
                    }}
                  >
                    {previewUrl ? (
                      <img src={previewUrl} alt="" style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
                    ) : null}
                  </div>
                  <p className="small text-muted mt-2 mb-0">
                    <strong>Dimensions:</strong> {dims}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="mw-modal-footer">
          <button type="button" className="mw-btn mw-btn-cancel" disabled={busy} onClick={onClose}>
            <i className="fa fa-times" /> Cancel
          </button>
          <button type="button" className="mw-btn mw-btn-save" disabled={busy} onClick={save}>
            <i className="fa fa-check-circle" /> {busy ? 'Uploading…' : 'Crop & Save'}
          </button>
        </div>
      </div>
    </div>
  )
}
