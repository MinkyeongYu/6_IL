type Handler<T> = (payload: T) => void;

export class EventBus<EventMap extends Record<string, unknown>> {
  private readonly listeners = new Map<keyof EventMap, Set<Handler<EventMap[keyof EventMap]>>>();

  on<K extends keyof EventMap>(event: K, handler: Handler<EventMap[K]>): () => void {
    let set = this.listeners.get(event);
    if (!set) {
      set = new Set();
      this.listeners.set(event, set);
    }
    set.add(handler as Handler<EventMap[keyof EventMap]>);

    return () => {
      this.listeners.get(event)?.delete(handler as Handler<EventMap[keyof EventMap]>);
    };
  }

  emit<K extends keyof EventMap>(event: K, payload: EventMap[K]): void {
    const set = this.listeners.get(event);
    if (!set) return;
    for (const h of set) {
      try {
        (h as Handler<EventMap[K]>)(payload);
      } catch (err) {
        console.error(`[EventBus] listener for "${String(event)}" threw:`, err);
      }
    }
  }

  clear(): void {
    this.listeners.clear();
  }
}
