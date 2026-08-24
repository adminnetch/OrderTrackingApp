﻿// odg.js — Gestione dinamica delle tabelle ODG

// 🔹 Inizializzazione quando il DOM è pronto
document.addEventListener("DOMContentLoaded", function () {
  // Attiva i campi Rich Text (ckeditor)
  document.querySelectorAll(".rich-text").forEach(editor => {
    ClassicEditor.create(editor).catch(error => console.error(error));
  });

  // Prende l’ID del progetto (serve per caricare i contatti troupe)
  const projInput = document.querySelector('input[name="CinemaOrderId"]');
  const projectId = projInput ? projInput.value : null;

  // Listener per bottone "Aggiungi Troupe"
  const btnTroupe = document.getElementById("addTroupe");
  if (btnTroupe && projectId) {
    btnTroupe.addEventListener("click", async () => {
      if (!window.troupeContacts || !window.troupeContacts.length) {
        try {
          const res = await fetch(`/TroupeCastContacts/GetForProject?projectId=${projectId}`);
          if (!res.ok) throw new Error("Fetch troupe failed");
          window.troupeContacts = await res.json();
        } catch (e) {
          console.error("Errore caricamento contatti troupe:", e);
          window.troupeContacts = [];
        }
      }
      addRow("troupeTable", "TroupeOrari", window.troupeContacts);
    });
  }

  // Listener per le altre tabelle dinamiche
  document.getElementById("addConvocazione").addEventListener("click", () =>
    addRow("convocazioniTable", "CastConvocazioni")
  );
  document.getElementById("addTrasporto").addEventListener("click", () =>
    addRow("trasportiTable", "Trasporti")
  );
  document.getElementById("addContatto").addEventListener("click", () =>
    addRow("contattiTable", "Contatti")
  );

  // 🔹 Imposta il comportamento dei <select> già presenti (Troupe)
  document.querySelectorAll("#troupeTable select").forEach(sel => {
    const roleIn = sel.closest("tr").querySelector('input[name$=".Ruolo"]');
    const selected = sel.selectedOptions[0];
    if (selected && selected.dataset.role && roleIn) {
      roleIn.value = selected.dataset.role;
    }
    sel.addEventListener("change", function () {
      roleIn.value = this.selectedOptions[0]?.dataset.role || "";
    });
  });
});

// 🔹 Funzione generica per aggiungere righe alle tabelle
function addRow(tableId, prefix, contacts = []) {
  const tbody = document.getElementById(tableId).querySelector("tbody");
  const idx = tbody.rows.length;
  const row = tbody.insertRow();

  if (tableId === "troupeTable") {
    const c0 = row.insertCell();

    // Campo nascosto Id = 0
    const idIn = document.createElement("input");
    idIn.type = "hidden";
    idIn.name = `${prefix}[${idx}].Id`;
    idIn.value = "0";
    c0.appendChild(idIn);

    const sel = document.createElement("select");
    sel.name = `${prefix}[${idx}].Nome`;
    sel.className = "form-select";
    sel.required = true;
    sel.innerHTML = `<option value="">-- seleziona --</option>` +
      contacts.map(c =>
        `<option value="${c.fullName}" data-role="${c.role}">
           ${c.fullName}
         </option>`).join("");
    c0.appendChild(sel);

    const c1 = row.insertCell();
    const roleIn = document.createElement("input");
    roleIn.type = "text";
    roleIn.readOnly = true;
    roleIn.name = `${prefix}[${idx}].Ruolo`;
    roleIn.className = "form-control";
    c1.appendChild(roleIn);

    const c2 = row.insertCell();
    const timeIn = document.createElement("input");
    timeIn.type = "time";
    timeIn.name = `${prefix}[${idx}].Orario`;
    timeIn.className = "form-control";
    c2.appendChild(timeIn);

    sel.addEventListener("change", function () {
      roleIn.value = this.selectedOptions[0]?.dataset.role || "";
    });

    const c3 = row.insertCell();
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "btn btn-danger remove-row";
    btn.innerText = "Rimuovi";
    btn.addEventListener("click", () => row.remove());
    c3.appendChild(btn);

    return;
  }

  // Configura i campi delle altre tabelle
  let fields = [];
  switch (tableId) {
    case "convocazioniTable":
      fields = [
        { name: "Attore",  type: "text" },
        { name: "PickUp",  type: "time" },
        { name: "Costume", type: "time" },
        { name: "Trucco",  type: "time" },
        { name: "Pronti",  type: "time" }
      ];
      break;
    case "trasportiTable":
      fields = [
        { name: "Auto", type: "text" },
        { name: "Chi",  type: "text" },
        { name: "Dove", type: "text" },
        { name: "Ora",  type: "time" }
      ];
      break;
    case "contattiTable":
      fields = [
        { name: "Nome",  type: "text" },
        { name: "Ruolo", type: "text" },
        { name: "Email", type: "text" }
      ];
      break;
  }

  fields.forEach((f, i) => {
    const cell = row.insertCell(i);
    const input = document.createElement("input");
    input.type = f.type;
    input.name = `${prefix}[${idx}].${f.name}`;
    input.className = "form-control";
    cell.appendChild(input);

    // Aggiungi campo Id nella prima cella
    if (i === 0) {
      const id = document.createElement("input");
      id.type = "hidden";
      id.name = `${prefix}[${idx}].Id`;
      id.value = "0";
      cell.appendChild(id);
    }
  });

  const remC = row.insertCell(fields.length);
  const remB = document.createElement("button");
  remB.type = "button";
  remB.className = "btn btn-danger remove-row";
  remB.innerText = "Rimuovi";
  remB.addEventListener("click", () => row.remove());
  remC.appendChild(remB);
}

// 🔹 Listener globale per rimuovere righe cliccando "Rimuovi"
document.addEventListener("click", function (e) {
  if (e.target.classList.contains("remove-row")) {
    e.target.closest("tr").remove();
  }
});

// 🔍 Log dei dati inviati al submit (debug utile)
document.addEventListener("DOMContentLoaded", function () {
  const form = document.querySelector('form[asp-action="Create"], form[asp-action="Edit"]');
  if (!form) return;
  form.addEventListener("submit", function () {
    const fd = new FormData(this);
    console.group("⚙️ FormData payload al submit");
    for (let [k, v] of fd.entries()) console.log(k, ":", v);
    console.groupEnd();
  });
});
