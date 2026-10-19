/*
 * Cities: Skylines II UI Module
 * Id: DriveTheCityII
 * Author: Drive the City II (private project)
 * Version: 0.1.0
 * Dependencies:
 */
// In-game HUD and panels for Drive the City II. All text arrives already localized (English / PT-BR) in the
// "driveTheCityII.hud" JSON view model published by HudUISystem (C#). Buttons only send action ids back through the
// "driveTheCityII.action" trigger; the UI never changes game or career state itself.

const api = window["cs2/api"];
const React = window.React;
const h = React.createElement;

const GROUP = "driveTheCityII";
const hudBinding = api.bindValue(GROUP, "hud", "{\"mode\":\"Building\"}");

function send(id) {
  api.trigger(GROUP, "action", id);
}

function useHud() {
  const raw = api.useValue(hudBinding);
  return React.useMemo(() => {
    try {
      return JSON.parse(raw || "{}");
    } catch (e) {
      return { mode: "Building" };
    }
  }, [raw]);
}

// Sizes use rem, where the game defines 1rem = 1 pixel at 1080p (html{font-size:.0925926vh}).
const palette = {
  card: "rgba(18, 24, 33, 0.82)",
  cardBorder: "rgba(255, 255, 255, 0.10)",
  text: "#f2f5f8",
  muted: "rgba(242, 245, 248, 0.66)",
  accent: "#4cc2ff",
  warn: "#ffb347",
  good: "#6fdc8c",
  bad: "#ff6b6b",
};

const styles = {
  card: {
    background: palette.card,
    border: "0.99rem solid " + palette.cardBorder,
    borderRadius: "8rem",
    color: palette.text,
    padding: "10rem 14rem",
    fontSize: "15.01rem",
  },
  label: { color: palette.muted, fontSize: "12rem", textTransform: "uppercase", letterSpacing: "0.5rem" },
  button: {
    background: "rgba(76, 194, 255, 0.18)",
    border: "0.99rem solid rgba(76, 194, 255, 0.55)",
    color: palette.text,
    borderRadius: "6rem",
    padding: "4.99rem 10rem",
    margin: "2rem 0 2rem 6rem",
    fontSize: "12.99rem",
    pointerEvents: "auto",
  },
  buttonDisabled: { opacity: 0.4 },
};

function Bar({ value, color }) {
  const pct = Math.max(0, Math.min(1, value || 0)) * 100;
  return h("div", { style: { width: "120rem", height: "6rem", background: "rgba(255,255,255,0.15)", borderRadius: "3.01rem", marginTop: "3.01rem" } },
    h("div", { style: { width: pct + "%", height: "6rem", background: color, borderRadius: "3.01rem" } }));
}

function StatusCard({ hud }) {
  const rows = [];
  rows.push(h("div", { key: "mode", style: { display: "flex", alignItems: "center" } },
    h("div", { style: { fontSize: "16rem", fontWeight: "bold" } }, hud.modeLabel || ""),
    hud.sandbox ? h("div", { style: { marginLeft: "8rem", padding: "0.99rem 6rem", borderRadius: "4rem", background: palette.warn, color: "#1b1b1b", fontSize: "11.01rem", fontWeight: "bold" } }, hud.sandboxLabel) : null,
    hud.paused ? h("div", { style: { marginLeft: "8rem", color: palette.warn, fontSize: "12rem" } }, hud.pausedLabel) : null));
  if (hud.speed) {
    rows.push(h("div", { key: "speed", style: { display: "flex", alignItems: "flex-end", marginTop: "4rem" } },
      h("div", { style: { fontSize: "30rem", fontWeight: "bold" } }, hud.speed),
      h("div", { style: { marginLeft: "10rem", fontSize: "18rem", color: palette.accent } }, hud.gear || "")));
  }
  if (hud.vehicleName) {
    rows.push(h("div", { key: "veh", style: { color: palette.muted, fontSize: "12rem" } }, hud.vehicleName));
  }
  if (typeof hud.fuel === "number") {
    rows.push(h("div", { key: "fuel", style: { marginTop: "6rem" } },
      h("div", { style: styles.label }, hud.fuelLabel || "Fuel"),
      h(Bar, { value: hud.fuel, color: hud.fuel < 0.15 ? palette.bad : palette.good })));
  }
  if (typeof hud.condition === "number") {
    rows.push(h("div", { key: "cond", style: { marginTop: "4rem" } },
      h("div", { style: styles.label }, hud.conditionLabel || "Condition"),
      h(Bar, { value: hud.condition, color: hud.condition < 0.3 ? palette.bad : palette.accent })));
  }
  if (hud.money) {
    rows.push(h("div", { key: "money", style: { marginTop: "8rem", display: "flex", justifyContent: "space-between" } },
      h("div", { style: { fontSize: "16rem", color: palette.good } }, hud.money),
      h("div", { style: { fontSize: "12.99rem", color: palette.muted, marginLeft: "12rem" } }, hud.level || "")));
  }
  return h("div", { style: Object.assign({}, styles.card, { position: "absolute", left: "20rem", top: "120rem", minWidth: "190rem", pointerEvents: "none" }) }, rows);
}

function ObjectiveCard({ hud }) {
  const o = hud.objective;
  if (!o) {
    return null;
  }
  return h("div", { style: Object.assign({}, styles.card, { position: "absolute", right: "20rem", top: "120rem", maxWidth: "380rem", pointerEvents: "none" }) },
    h("div", { style: styles.label }, o.label || ""),
    h("div", { style: { fontSize: "16.99rem", fontWeight: "bold", marginTop: "2rem" } }, o.title || ""),
    o.detail ? h("div", { style: { color: palette.muted, marginTop: "3.01rem" } }, o.detail) : null,
    o.distance ? h("div", { style: { color: palette.accent, marginTop: "4rem", fontSize: "18rem" } }, o.distance) : null,
    o.reward ? h("div", { style: { color: palette.good, marginTop: "2rem" } }, o.reward) : null);
}

function PromptBar({ hud }) {
  const prompts = hud.prompts || [];
  return h("div", { style: { position: "absolute", left: "0rem", right: "0rem", bottom: "160rem", display: "flex", flexDirection: "column", alignItems: "center", pointerEvents: "none" } },
    hud.message ? h("div", { style: Object.assign({}, styles.card, { marginBottom: "8rem", maxWidth: "760rem", textAlign: "center", borderColor: "rgba(76,194,255,0.5)" }) }, hud.message) : null,
    prompts.length > 0 ? h("div", { style: Object.assign({}, styles.card, { padding: "6rem 12rem", fontSize: "12.99rem", color: palette.muted }) }, prompts.join("   ·   ")) : null,
    hud.panelHint && !hud.panelOpen ? h("div", { style: { marginTop: "4rem", fontSize: "12rem", color: palette.muted } }, hud.panelHint) : null);
}

function Panel({ hud }) {
  const panel = hud.panel;
  if (!hud.panelOpen || !panel) {
    return null;
  }
  const tabs = (panel.tabs || []).map((t) =>
    h("div", {
      key: t.id,
      onClick: () => send("tab:" + t.id),
      style: { padding: "6rem 12rem", marginRight: "4rem", borderRadius: "6rem 6rem 0 0", cursor: "pointer", pointerEvents: "auto",
        background: t.active ? "rgba(76,194,255,0.25)" : "rgba(255,255,255,0.06)", color: t.active ? palette.text : palette.muted },
    }, t.label));
  const rows = (panel.rows || []).map((r) =>
    h("div", { key: r.id || r.title, style: { display: "flex", alignItems: "center", justifyContent: "space-between", padding: "7.01rem 4rem", borderBottom: "0.99rem solid rgba(255,255,255,0.07)", background: r.highlight ? "rgba(111,220,140,0.10)" : "transparent" } },
      h("div", { style: { flex: "1" } },
        h("div", { style: { fontSize: "14rem" } }, r.title),
        r.subtitle ? h("div", { style: { fontSize: "12rem", color: palette.muted, marginTop: "2rem" } }, r.subtitle) : null),
      r.right ? h("div", { style: { fontSize: "12.99rem", color: palette.accent, marginLeft: "10rem" } }, r.right) : null,
      h("div", { style: { display: "flex" } },
        (r.actions || []).map((a) => h("div", {
          key: a.id,
          onClick: () => { if (a.enabled) { send(a.id); } },
          style: Object.assign({}, styles.button, a.enabled ? {} : styles.buttonDisabled),
        }, a.label)))));
  return h("div", { style: Object.assign({}, styles.card, { position: "absolute", right: "20rem", top: "300rem", width: "560rem", maxHeight: "560rem", pointerEvents: "auto", display: "flex", flexDirection: "column" }) },
    h("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "center" } },
      h("div", { style: { display: "flex" } }, tabs),
      h("div", { onClick: () => send("panel"), style: Object.assign({}, styles.button, { margin: "0" }) }, "×")),
    panel.header ? h("div", { style: { fontSize: "16rem", fontWeight: "bold", margin: "8rem 0 4rem 0" } }, panel.header) : null,
    h("div", { style: { overflowY: "scroll", flex: "1" } }, rows),
    panel.footer ? h("div", { style: { fontSize: "12rem", color: palette.muted, marginTop: "6rem" } }, panel.footer) : null);
}

function Hud() {
  const hud = useHud();
  if (!hud.city || hud.mode === "Building") {
    return null;
  }
  return h("div", { style: { position: "absolute", left: "0rem", top: "0rem", right: "0rem", bottom: "0rem", pointerEvents: "none" } },
    h(StatusCard, { hud }),
    h(ObjectiveCard, { hud }),
    h(PromptBar, { hud }),
    h(Panel, { hud }));
}

function ToggleButton() {
  const hud = useHud();
  if (!hud.city || hud.mode !== "Building") {
    return null;
  }
  return h("div", {
    onClick: () => send("toggle"),
    style: Object.assign({}, styles.button, { margin: "6rem", padding: "6rem 12rem", background: "rgba(18,24,33,0.85)" }),
  }, hud.toggleLabel || "Drive the City");
}

const register = (moduleRegistry) => {
  moduleRegistry.append("Game", Hud);
  moduleRegistry.append("GameTopLeft", ToggleButton);
};

export default register;
