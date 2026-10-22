// Browser geometry and responsive pointer preview; authoritative values/callbacks remain in C#.
export function connect(root, dotnet) {
    const track = root.querySelector('.admiral-slider__track');
    let config, values = [], gesture, packets = [], sending = false, timer, disposed = false;
    let active = 0;
    const handles = () => [...track.querySelectorAll('[data-handle]')];
    const clamp = value => Math.max(config.min, Math.min(config.max, value));
    function decimalPlaces(value) {
        const [coefficient, exponent = '0'] = String(value).toLowerCase().split('e');
        return Math.max(0, (coefficient.split('.')[1]?.length || 0) - Number(exponent));
    }
    function normalize(value) {
        value = clamp(value);
        return config.any || value === config.min || value === config.max ? value
            : clamp(Number((config.min + Math.floor((value - config.min) / config.step + 0.5000000001) * config.step)
                .toFixed(Math.max(decimalPlaces(config.min), decimalPlaces(config.step)))));
    }
    function draw() {
        if (!config || disposed) return;
        const percent = value => (value - config.min) / (config.max - config.min) * 100;
        const start = config.range ? percent(values[0]) : 0;
        track.style.setProperty('--admiral-slider-fill-start', start + '%');
        track.style.setProperty('--admiral-slider-fill-width', (percent(values.at(-1)) - start) + '%');
        handles().forEach((handle, index) => {
            track.style.setProperty(`--admiral-slider-position-${index}`, percent(values[index]) + '%');
            handle.setAttribute('aria-valuenow', String(values[index]));
            handle.classList.toggle('admiral-slider__handle--dragging', !!gesture && active === index);
        });
    }
    function schedule(delay = 35) {
        if (!disposed && !sending && packets.length && !timer) timer = setTimeout(flush, delay);
    }
    async function flush() {
        timer = undefined;
        if (disposed || sending || !packets.length) return;
        const packet = packets.shift();
        sending = true;
        try {
            const accepted = await dotnet.invokeMethodAsync('UpdateFromBrowser', packet.values, packet.version, packet.finished, packet.cancelled);
            if (!gesture && !packets.length && accepted.version === packet.version && packet.version === config.version) {
                values = accepted.values;
                draw();
            }
        }
        catch { dispose(); }
        finally { sending = false; schedule(0); }
    }
    function publish(finished = false, cancelled = false, keyboard = false) {
        const packet = { values: [...values], version: config.version, finished, cancelled, keyboard };
        const last = packets.at(-1);
        // Coalesce pointer preview, but never drop a keyboard action or completion.
        if (!keyboard && last && !last.keyboard && !last.finished) packets[packets.length - 1] = packet;
        else packets.push(packet);
        if (finished) { clearTimeout(timer); timer = undefined; }
        schedule(finished || keyboard ? 0 : 35);
    }
    function setValue(value) {
        values[active] = normalize(value);
        if (config.range && values[0] > values[1]) {
            values.reverse();
            active = 1 - active;
            handles()[active]?.focus({ preventScroll: true });
        }
        draw();
    }
    function valueAt(event) {
        const rect = track.getBoundingClientRect();
        if (rect.width <= 0) return values[active];
        const rtl = getComputedStyle(track).direction === 'rtl';
        const ratio = (event.clientX - (gesture?.offset || 0) - rect.left) / rect.width;
        return config.min + (rtl ? 1 - ratio : ratio) * (config.max - config.min);
    }
    const down = event => {
        if (!config || config.blocked || gesture || packets.at(-1)?.finished || (event.pointerType === 'mouse' && event.button !== 0)) return;
        event.preventDefault();
        const value = valueAt(event);
        const handle = event.target.closest('[data-handle]');
        active = handle ? Number(handle.dataset.handle) : !config.range ? 0
            : Math.abs(value - values[0]) <= Math.abs(value - values[1]) ? (values[0] === values[1] && value >= values[0] ? 1 : 0) : 1;
        const rect = handle?.getBoundingClientRect();
        gesture = { id: event.pointerId, version: config.version, offset: rect ? event.clientX - rect.left - rect.width / 2 : 0 };
        handles()[active]?.focus({ preventScroll: true });
        try { track.setPointerCapture(event.pointerId); } catch { /* Synthetic pointer events. */ }
        // Starting on a thumb must not jump its center to an off-center pointer.
        if (!handle) { setValue(value); publish(); }
        draw();
    };
    const move = event => {
        if (!gesture || event.pointerId !== gesture.id) return;
        setValue(valueAt(event));
        publish();
    };
    const end = event => {
        if (!gesture || event.pointerId !== gesture.id) return;
        const id = gesture.id;
        const cancelled = event.type !== 'pointerup';
        if (!cancelled) setValue(valueAt(event));
        gesture = undefined;
        if (track.hasPointerCapture(id)) track.releasePointerCapture(id);
        draw();
        publish(true, cancelled);
    };
    const keydown = event => {
        const handle = event.target.closest('[data-handle]');
        if (!handle || !config || config.blocked || event.ctrlKey || event.metaKey || event.altKey || event.isComposing) return;
        if (packets.at(-1)?.finished) return;
        active = Number(handle.dataset.handle);
        const rtl = getComputedStyle(track).direction === 'rtl';
        const changes = { ArrowLeft: rtl ? 1 : -1, ArrowRight: rtl ? -1 : 1, ArrowUp: 1, ArrowDown: -1, PageUp: 10, PageDown: -10 };
        let value;
        if (event.key === 'Home') value = config.min;
        else if (event.key === 'End') value = config.max;
        else if (Object.hasOwn(changes, event.key)) value = values[active] + changes[event.key] * config.step;
        else return; // Tab and all other keys remain native.
        event.preventDefault();
        event.stopPropagation();
        setValue(value);
        publish(false, false, true);
    };
    track.addEventListener('pointerdown', down);
    track.addEventListener('pointermove', move);
    track.addEventListener('pointerup', end);
    track.addEventListener('pointercancel', end);
    track.addEventListener('lostpointercapture', end);
    root.addEventListener('keydown', keydown);
    function dispose() {
        disposed = true;
        clearTimeout(timer);
        gesture = undefined; packets = [];
        track.removeEventListener('pointerdown', down);
        track.removeEventListener('pointermove', move);
        track.removeEventListener('pointerup', end);
        track.removeEventListener('pointercancel', end);
        track.removeEventListener('lostpointercapture', end);
        root.removeEventListener('keydown', keydown);
    }
    return {
        update(next) {
            const changed = config?.version !== next.version;
            config = next;
            root.dataset.sliderStep = config.any ? 'any' : String(config.step);
            if (changed) {
                const pointer = gesture?.id;
                gesture = undefined; packets = [];
                clearTimeout(timer); timer = undefined;
                if (pointer != null && track.hasPointerCapture(pointer)) track.releasePointerCapture(pointer);
            }
            if (changed || (!gesture && !sending && !packets.length)) values = [...config.values];
            draw();
        },
        dispose
    };
}
