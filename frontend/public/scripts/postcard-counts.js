function fetchPostCardCounts() {
  var apiBase = document.querySelector('meta[name="api-base"]');
  if (!apiBase) return;
  var api = apiBase.getAttribute('content');
  document.querySelectorAll('.postcard-likes[data-slug]').forEach(function(el) {
    var slug = el.getAttribute('data-slug');
    if (!slug) return;
    var card = el.closest('article');

    fetch(api + '/api/posts/' + slug + '/likes/count')
      .then(function(res) { return res.ok ? res.json() : null; })
      .then(function(data) {
        if (!data) return;
        var countEl = el.querySelector('.postcard-like-count');
        if (countEl) countEl.textContent = String(data.count ?? data);
      })
      .catch(function() {});

    if (!card) return;
    fetch(api + '/api/posts/' + slug + '/comments/count')
      .then(function(res) { return res.ok ? res.json() : null; })
      .then(function(data) {
        if (!data) return;
        var countEl = card.querySelector('.postcard-comment-count');
        if (countEl) countEl.textContent = String(data.count ?? data);
      })
      .catch(function() {});
  });
}
fetchPostCardCounts();
document.addEventListener('astro:after-swap', fetchPostCardCounts);
