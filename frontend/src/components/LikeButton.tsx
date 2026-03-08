import { useState, useEffect, useRef, useCallback } from 'preact/hooks';

interface Props {
  slug: string;
  shareTitle?: string;
  shareUrl?: string;
}

function getApiBase(): string {
  return document.querySelector<HTMLMetaElement>('meta[name="api-base"]')?.content || 'http://localhost:5063';
}

function getVisitorId(): string {
  return (window as any).__visitorId || '';
}

const PARTICLE_ANGLES = [0, 45, 90, 135, 180, 225, 270, 315];

function isMobile(): boolean {
  return typeof window !== 'undefined' && /Mobi|Android|iPhone|iPad/i.test(navigator.userAgent);
}

function shareLinks(title: string, url: string) {
  const encoded = encodeURIComponent(url);
  const encodedTitle = encodeURIComponent(title);
  return [
    { label: 'X', href: `https://x.com/intent/tweet?text=${encodedTitle}&url=${encoded}`, icon: 'M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z' },
    { label: 'LinkedIn', href: `https://www.linkedin.com/sharing/share-offsite/?url=${encoded}`, icon: 'M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z' },
    { label: 'WhatsApp', href: `https://wa.me/?text=${encodedTitle}%20${encoded}`, icon: 'M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z' },
    { label: 'Email', href: `mailto:?subject=${encodedTitle}&body=${encoded}`, icon: 'M1.5 8.67v8.58a3 3 0 003 3h15a3 3 0 003-3V8.67l-8.928 5.493a3 3 0 01-3.144 0L1.5 8.67z M22.5 6.908V6.75a3 3 0 00-3-3h-15a3 3 0 00-3 3v.158l9.714 5.978a1.5 1.5 0 001.572 0L22.5 6.908z' },
  ];
}

export default function LikeButton({ slug, shareTitle, shareUrl }: Props) {
  const [liked, setLiked] = useState(false);
  const [count, setCount] = useState(0);
  const [pulsing, setPulsing] = useState(false);
  const [showParticles, setShowParticles] = useState(false);
  const [copied, setCopied] = useState(false);
  const [shareOpen, setShareOpen] = useState(false);
  const heartRef = useRef<SVGSVGElement>(null);
  const shareRef = useRef<HTMLDivElement>(null);

  const fetchState = useCallback(async () => {
    const api = getApiBase();
    const visitorId = getVisitorId();
    if (!visitorId) return;

    try {
      const [countRes, checkRes] = await Promise.all([
        fetch(`${api}/api/posts/${slug}/likes`),
        fetch(`${api}/api/posts/${slug}/likes/check/${visitorId}`),
      ]);
      if (countRes.ok) {
        const data = await countRes.json();
        setCount(data.count ?? data);
      }
      if (checkRes.ok) {
        const data = await checkRes.json();
        setLiked(data.liked ?? false);
      }
    } catch {
      // Silently fail on network errors — non-critical feature
    }
  }, [slug]);

  useEffect(() => {
    fetchState();
    document.addEventListener('astro:after-swap', fetchState);
    return () => document.removeEventListener('astro:after-swap', fetchState);
  }, [fetchState]);

  const handleClick = async () => {
    const api = getApiBase();
    const visitorId = getVisitorId();
    if (!visitorId) return;

    const wasLiked = liked;
    setLiked(!wasLiked);
    setCount(prev => wasLiked ? Math.max(0, prev - 1) : prev + 1);

    setPulsing(true);
    if (!wasLiked) setShowParticles(true);
    setTimeout(() => setPulsing(false), 400);
    setTimeout(() => setShowParticles(false), 600);

    try {
      const url = `${api}/api/posts/${slug}/likes`;
      const res = wasLiked
        ? await fetch(`${url}/${visitorId}`, { method: 'DELETE' })
        : await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ visitorId }),
          });

      if (!res.ok) {
        setLiked(wasLiked);
        setCount(prev => wasLiked ? prev + 1 : Math.max(0, prev - 1));
      }
    } catch {
      setLiked(wasLiked);
      setCount(prev => wasLiked ? prev + 1 : Math.max(0, prev - 1));
    }
  };

  useEffect(() => {
    if (!shareOpen) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (shareRef.current && !shareRef.current.contains(e.target as Node)) {
        setShareOpen(false);
      }
    };
    document.addEventListener('click', handleClickOutside, true);
    return () => document.removeEventListener('click', handleClickOutside, true);
  }, [shareOpen]);

  const handleShare = useCallback(async () => {
    if (!shareUrl) return;

    if (isMobile() && typeof navigator !== 'undefined' && navigator.share) {
      try {
        await navigator.share({ title: shareTitle, url: shareUrl });
        return;
      } catch {
        // User cancelled
      }
      return;
    }

    setShareOpen(prev => !prev);
  }, [shareTitle, shareUrl]);

  const handleCopy = useCallback(async () => {
    if (!shareUrl) return;
    try {
      await navigator.clipboard.writeText(shareUrl);
      setCopied(true);
      setTimeout(() => { setCopied(false); setShareOpen(false); }, 1500);
    } catch {}
  }, [shareUrl]);

  const heartClasses = [
    'like-heart',
    liked ? 'like-heart--liked' : '',
    pulsing ? 'like-heart--pulse' : '',
  ].filter(Boolean).join(' ');

  return (
    <div className="like-sidebar" role="group" aria-label="Post engagement">
      <button
        onClick={handleClick}
        aria-label={liked ? 'Unlike this post' : 'Like this post'}
        aria-pressed={liked}
        style={{ position: 'relative', background: 'none', border: 'none', padding: 0, cursor: 'pointer' }}
      >
        <svg
          ref={heartRef}
          className={heartClasses}
          viewBox="0 0 24 24"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
        </svg>
        {showParticles && (
          <div className="like-particles" aria-hidden="true">
            {PARTICLE_ANGLES.map((angle, i) => {
              const rad = (angle * Math.PI) / 180;
              const dist = 18;
              const x = Math.cos(rad) * dist;
              const y = Math.sin(rad) * dist;
              return (
                <span
                  key={i}
                  className="like-particle"
                  style={{
                    left: '50%',
                    top: '50%',
                    '--particle-end': `translate(${x}px, ${y}px)`,
                    animationDelay: `${i * 30}ms`,
                  } as any}
                />
              );
            })}
          </div>
        )}
      </button>
      <span className={`like-count ${liked ? 'like-count--liked' : ''}`}>{count}</span>

      {shareUrl && (
        <>
          <div className="like-divider" aria-hidden="true" />
          <div ref={shareRef} style={{ position: 'relative' }}>
            <button
              className="share-button"
              onClick={handleShare}
              aria-label="Share this post"
              aria-expanded={shareOpen}
            >
              <svg viewBox="0 0 24 24" stroke-linecap="round" stroke-linejoin="round">
                <path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8" />
                <polyline points="16 6 12 2 8 6" />
                <line x1="12" y1="2" x2="12" y2="15" />
              </svg>
            </button>

            {shareOpen && shareTitle && (
              <div className="share-popover" role="menu">
                <p className="share-popover-title">Share</p>
                <div className="share-popover-links">
                  {shareLinks(shareTitle, shareUrl).map(({ label, href, icon }) => (
                    <a
                      key={label}
                      href={href}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="share-popover-link"
                      role="menuitem"
                      onClick={() => setShareOpen(false)}
                    >
                      <svg viewBox="0 0 24 24" fill="currentColor"><path d={icon} /></svg>
                      <span>{label}</span>
                    </a>
                  ))}
                </div>
                <button className="share-popover-copy" onClick={handleCopy}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                    <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" />
                  </svg>
                  <span>{copied ? 'Copied!' : 'Copy link'}</span>
                </button>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
