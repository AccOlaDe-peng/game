const screens = [...document.querySelectorAll('.screen')];
let current = 'menu', previous = 'menu';
function show(name) { previous = current; current = name; screens.forEach(s => s.classList.toggle('active', s.dataset.screen === name)); }
document.addEventListener('click', e => {
  const go = e.target.closest('[data-go]'); if (go) show(go.dataset.go);
  if (e.target.closest('[data-back]')) show(previous === 'settings' ? 'menu' : previous);
  const card = e.target.closest('.character:not(.locked)');
  if (card) { document.querySelectorAll('.character').forEach(c => c.classList.remove('selected')); card.classList.add('selected'); document.querySelector('#selection-name').textContent = `${card.dataset.character} · ${card.dataset.weapon}`; }
  const upgrade = e.target.closest('.upgrade'); if (upgrade) { document.querySelectorAll('.upgrade').forEach(c => c.classList.remove('chosen')); upgrade.classList.add('chosen'); setTimeout(() => show('hud'), 350); }
});
document.querySelectorAll('input[type=range]').forEach(input => input.addEventListener('input', () => input.parentElement.querySelector('output').value = `${input.value}%`));
