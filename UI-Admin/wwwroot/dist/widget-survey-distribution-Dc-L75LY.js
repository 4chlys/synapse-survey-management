function a(e){const i=e.dataset.surveyUrl??"",t=e.dataset.projectId??"",o=`https://chart.googleapis.com/chart?cht=qr&chs=200x200&chl=${encodeURIComponent(i)}`;e.innerHTML=`
        <p class="text-xs text-gray-400 mb-2">Survey-link</p>
        <div id="url-row-${t}" class="flex items-center gap-2 mb-3">
            <span id="url-text-${t}" class="text-gray-200 text-xs break-all flex-1"></span>
            <button id="copy-btn-${t}"
                class="bg-gray-700 hover:bg-gray-600 text-white text-xs px-2 py-1 rounded whitespace-nowrap transition">
                Kopieer
            </button>
        </div>
        <img id="qr-img-${t}" alt="QR code" class="rounded mb-2" width="160" height="160" />
        <a id="qr-link-${t}" download="qr-code.png" class="text-xs text-gray-400 hover:text-white underline">
            QR downloaden
        </a>
    `;const n=e.querySelector(`#url-text-${t}`);n&&(n.textContent=i);const s=e.querySelector(`#qr-img-${t}`);s&&(s.src=o);const c=e.querySelector(`#qr-link-${t}`);c&&(c.href=o);const r=e.querySelector(`#copy-btn-${t}`);r==null||r.addEventListener("click",()=>{navigator.clipboard.writeText(i).then(()=>{r&&(r.textContent="Gekopieerd!"),setTimeout(()=>{r&&(r.textContent="Kopieer")},2e3)})})}export{a as i};
