import { useState, useEffect, useRef, useCallback } from 'preact/hooks';

interface Props {
  filePath: string;
  initialContent: string;
  renderedHtml: string;
  editMode?: boolean;
}

type Mode = 'preview' | 'edit';
type SaveStatus = 'idle' | 'saving' | 'saved' | 'error';

function getApiBase(): string {
  return document.querySelector<HTMLMetaElement>('meta[name="api-base"]')?.content || 'http://localhost:5063';
}

const ADMIN_KEY = 'dev-admin-key';
const DEBOUNCE_MS = 1500;

export default function PostEditor({ filePath, initialContent, renderedHtml, editMode = false }: Props) {
  const [mode, setMode] = useState<Mode>(editMode ? 'edit' : 'preview');
  const [content, setContent] = useState(initialContent);
  const [saveStatus, setSaveStatus] = useState<SaveStatus>('idle');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const previewRef = useRef<HTMLDivElement>(null);

  // Populate the preview div once using DOM APIs (trusted SSR output from Shiki+remark)
  useEffect(() => {
    if (!previewRef.current) return;
    const parser = new DOMParser();
    const doc = parser.parseFromString(renderedHtml, 'text/html');
    const nodes = Array.from(doc.body.childNodes);
    previewRef.current.replaceChildren(...nodes);
  }, [renderedHtml]);

  const save = useCallback(async (text: string): Promise<boolean> => {
    setSaveStatus('saving');
    try {
      const res = await fetch(`${getApiBase()}/local/file`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Key': ADMIN_KEY,
        },
        body: JSON.stringify({ filePath, content: text }),
      });
      if (res.ok) {
        setSaveStatus('saved');
        return true;
      }
      setSaveStatus('error');
      return false;
    } catch {
      setSaveStatus('error');
      return false;
    }
  }, [filePath]);

  const scheduleAutoSave = useCallback((text: string) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => save(text), DEBOUNCE_MS);
  }, [save]);

  const handleChange = useCallback((e: Event) => {
    const text = (e.target as HTMLTextAreaElement).value;
    setContent(text);
    setSaveStatus('idle');
    scheduleAutoSave(text);
  }, [scheduleAutoSave]);

  const refreshPreview = useCallback(async (text: string) => {
    try {
      const res = await fetch('/api/render', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: text }),
      });
      if (!res.ok) return;
      const { html } = await res.json();
      if (!previewRef.current) return;
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, 'text/html');
      previewRef.current.replaceChildren(...Array.from(doc.body.childNodes));
      setMode('preview');
    } catch {
      // silently fail — user stays in edit mode
    }
  }, []);

  useEffect(() => {
    if (mode !== 'edit') return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 's') {
        e.preventDefault();
        refreshPreview(content);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [mode, content, refreshPreview]);

  useEffect(() => {
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, []);

  const statusLabel: Record<SaveStatus, string> = {
    idle: '',
    saving: 'Saving…',
    saved: 'Saved ✓',
    error: 'Error ✗',
  };

  const statusColor: Record<SaveStatus, string> = {
    idle: 'transparent',
    saving: 'var(--color-text-muted)',
    saved: '#4ade80',
    error: 'var(--color-accent)',
  };

  return (
    <div>
      {/* Toolbar */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: '0.75rem',
        marginBottom: '1.5rem',
        padding: '0.5rem 0.75rem',
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: '0.375rem',
        fontFamily: 'var(--font-mono)',
        fontSize: '0.75rem',
      }}>
        <button
          onClick={() => setMode(mode === 'edit' ? 'preview' : 'edit')}
          style={{
            padding: '0.25rem 0.75rem',
            background: mode === 'edit' ? 'var(--color-accent)' : 'var(--color-surface)',
            color: mode === 'edit' ? 'var(--color-bg)' : 'var(--color-text)',
            border: '1px solid var(--color-border)',
            borderRadius: '0.25rem',
            cursor: 'pointer',
            fontFamily: 'inherit',
            fontSize: 'inherit',
            fontWeight: 600,
          }}
        >
          {mode === 'edit' ? '\u2190 Preview' : 'Edit'}
        </button>

        {mode === 'edit' && (
          <button
            onClick={() => refreshPreview(content)}
            style={{
              padding: '0.25rem 0.75rem',
              background: 'var(--color-surface)',
              color: 'var(--color-text)',
              border: '1px solid var(--color-border)',
              borderRadius: '0.25rem',
              cursor: 'pointer',
              fontFamily: 'inherit',
              fontSize: 'inherit',
            }}
          >
            Refresh Preview
          </button>
        )}

        {mode === 'edit' && saveStatus !== 'idle' && (
          <span style={{ color: statusColor[saveStatus], marginLeft: 'auto' }}>
            {statusLabel[saveStatus]}
          </span>
        )}

        {mode === 'edit' && (
          <span style={{ color: 'var(--color-text-muted)', marginLeft: saveStatus === 'idle' ? 'auto' : '0' }}>
            Cmd+S to refresh preview
          </span>
        )}
      </div>

      {/* Preview div — always in DOM, populated once via DOMParser, hidden in edit mode */}
      <div
        ref={previewRef}
        class="prose-forge"
        style={{ display: mode === 'preview' ? 'block' : 'none' }}
      />

      {/* Edit textarea — only mounted in edit mode */}
      {mode === 'edit' && (
        <textarea
          value={content}
          onInput={handleChange}
          spellcheck={false}
          style={{
            width: '100%',
            minHeight: '70vh',
            padding: '1rem',
            background: 'var(--color-surface)',
            color: 'var(--color-text)',
            border: '1px solid var(--color-border)',
            borderRadius: '0.375rem',
            fontFamily: 'var(--font-mono)',
            fontSize: '0.875rem',
            lineHeight: '1.6',
            resize: 'vertical',
            outline: 'none',
            boxSizing: 'border-box',
          }}
        />
      )}
    </div>
  );
}
