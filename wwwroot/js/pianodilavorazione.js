// ✅ Funzione per aggiungere un Giorno di Ripresa
function aggiungiGiorno() {
    const container = document.getElementById("giorniRipresaContainer");
    const giornoIndex = container.children.length;

    const giornoDiv = document.createElement("div");
    giornoDiv.classList.add("card", "mb-3", "p-3");
    giornoDiv.innerHTML = `
        <h5>Giorno di Ripresa #${giornoIndex + 1}</h5>

        <div class="form-group mb-2">
            <label>Numero Giorno</label>
            <input type="number" name="GiorniRipresa[${giornoIndex}].NumeroGiorno" class="form-control" required />
        </div>

        <div class="form-group mb-2">
            <label>Osservazioni</label>
            <textarea name="GiorniRipresa[${giornoIndex}].Osservazioni" class="form-control"></textarea>
        </div>

        <hr />
        <h6>Scene</h6>
        <div id="sceneContainer${giornoIndex}"></div>
        <button type="button" class="btn btn-outline-primary btn-sm mb-3" onclick="aggiungiScena(${giornoIndex})">➕ Aggiungi Scena</button>

        <hr />
        <h6>Attori</h6>
        <div id="attoriContainer${giornoIndex}"></div>
        <button type="button" class="btn btn-outline-success btn-sm mb-3" onclick="aggiungiAttore(${giornoIndex})">➕ Aggiungi Attore</button>

        <hr />
        <h6>Locations</h6>
        <div id="locationsContainer${giornoIndex}"></div>
        <button type="button" class="btn btn-outline-warning btn-sm mb-3" onclick="aggiungiLocation(${giornoIndex})">➕ Aggiungi Location</button>

        <div class="text-end mt-3">
            <button type="button" class="btn btn-danger btn-sm" onclick="this.closest('.card').remove()">🗑️ Rimuovi Giorno</button>
        </div>
    `;

    container.appendChild(giornoDiv);
}

// ✅ Funzione per aggiungere Scena
function aggiungiScena(giornoIndex) {
    const container = document.getElementById(`sceneContainer${giornoIndex}`);
    const sceneIndex = container.children.length;

    const div = document.createElement("div");
    div.classList.add("input-group", "mb-2");
    div.innerHTML = `
        <input type="text" name="GiorniRipresa[${giornoIndex}].Scene[${sceneIndex}].NumeroScena" class="form-control" placeholder="Numero Scena (es: 1)" required />
        <input type="text" name="GiorniRipresa[${giornoIndex}].Scene[${sceneIndex}].Descrizione" class="form-control" placeholder="Descrizione" />
        <button type="button" class="btn btn-outline-danger" onclick="this.parentElement.remove()">🗑️</button>
    `;
    container.appendChild(div);
}

// ✅ Funzione per aggiungere Attore
function aggiungiAttore(giornoIndex) {
    const container = document.getElementById(`attoriContainer${giornoIndex}`);
    const attoreIndex = container.children.length;

    const div = document.createElement("div");
    div.classList.add("input-group", "mb-2");
    div.innerHTML = `
        <input type="text" name="GiorniRipresa[${giornoIndex}].Attori[${attoreIndex}].NomeAttore" class="form-control" placeholder="Nome Attore" required />
        <button type="button" class="btn btn-outline-danger" onclick="this.parentElement.remove()">🗑️</button>
    `;
    container.appendChild(div);
}

// ✅ Funzione per aggiungere Location
function aggiungiLocation(giornoIndex) {
    const container = document.getElementById(`locationsContainer${giornoIndex}`);
    const locationIndex = container.children.length;

    const div = document.createElement("div");
    div.classList.add("input-group", "mb-2");
    div.innerHTML = `
        <input type="text" name="GiorniRipresa[${giornoIndex}].Locations[${locationIndex}].NomeLocation" class="form-control" placeholder="Nome Location" required />
        <select name="GiorniRipresa[${giornoIndex}].Locations[${locationIndex}].TipoLocation" class="form-control">
            <option value="INT">INT</option>
            <option value="EXT">EXT</option>
            <option value="INT/EXT">INT/EXT</option>
        </select>
        <button type="button" class="btn btn-outline-danger" onclick="this.parentElement.remove()">🗑️</button>
    `;
    container.appendChild(div);
}

// ✅ Associa il bottone principale all'aggiunta Giorno
document.addEventListener("DOMContentLoaded", function () {
    const addButton = document.getElementById("addGiornoBtn");
    if (addButton) {
        addButton.addEventListener("click", aggiungiGiorno);
    }
});
