function u(e,t){return`${e??""}|${t??""}`}function v(e){const t=new Map;for(const n of e){const r=u(n.parentQuestionId,n.parentTriggerValue),o=t.get(r);o?o.push(n):t.set(r,[n])}for(const n of t.values())n.sort((r,o)=>r.position-o.position);return t}function x(e){return e.questionFormat==="SingleChoice"||e.questionFormat==="MultipleChoice"?e.answerChoices:e.questionFormat==="Likert"?Array.from({length:10},(t,n)=>String(n+1)):[]}function a(e){const t=document.createElement("div");return t.textContent=e,t.innerHTML}function c(e,t,n,r){const o=x(e),s=o.length>0,d=s?t===0?"v":">":"",l=t===0?"":"hidden",p=`${r.deleteEndpoint}?projectId=${r.projectId}&questionId=${e.questionId}&aiAssistance=${encodeURIComponent(r.aiAssistance)}`,g=s?o.map(i=>{const m=n.get(u(e.questionId,i))??[],y=e.endingAnswers.includes(i);return`
                <div class="answer-group pl-4 border-l border-gray-700 ml-2 mt-2">
                    <div class="flex items-center gap-2 py-1 text-gray-300">
                        <span class="flex-1">↳ ${a(i)}</span>
                        <label class="text-xs text-gray-400 flex items-center gap-1">
                            <input type="checkbox"
                                   class="ending-answer-toggle"
                                   data-question-id="${e.questionId}"
                                   data-trigger-value="${a(i)}"
                                   ${y?"checked":""} />
                            Beëindig enquête
                        </label>
                    </div>
                    ${m.map(b=>c(b,t+1,n,r)).join("")}
                    <div class="drop-zone border border-dashed border-gray-600 text-gray-500 text-center py-2 my-1 text-xs"
                         data-parent-id="${e.questionId}"
                         data-parent-trigger="${a(i)}">
                        Sleep hier om een subvraag aan dit antwoord toe te voegen
                    </div>
                </div>`}).join(""):"";return`
        <div class="question-card border border-gray-500 bg-gray-900 mb-2"
             draggable="true"
             data-question-id="${e.questionId}"
             data-format="${a(e.questionFormat)}"
             data-parent-id="${e.parentQuestionId??""}"
             data-parent-trigger="${a(e.parentTriggerValue??"")}"
             data-position="${e.position}"
             style="margin-left:${t*16}px">

            <div class="flex items-stretch">
                <span class="question-grip px-2 py-3 cursor-move text-gray-400 select-none" title="Versleep om te verplaatsen">⠿</span>
                <button type="button"
                        class="question-toggle flex-1 px-3 py-3 text-left flex justify-between items-center hover:bg-gray-800 transition"
                        data-question-id="${e.questionId}">
                    <span>${a(e.text)} <span class="text-gray-500 ml-2">[${a(e.questionFormat)}]</span></span>
                    <span id="arrow-${e.questionId}" class="text-gray-300 ml-2">${d}</span>
                </button>
                <button type="button"
                        class="question-edit-btn border-l border-gray-500 px-3 text-gray-400 hover:text-white hover:bg-gray-700 transition"
                        data-question-id="${e.questionId}"
                        data-text="${a(e.text)}"
                        data-format="${a(e.questionFormat)}"
                        data-answers='${a(JSON.stringify(e.answerChoices))}'
                        data-section-id="${e.sectionId??""}">✎</button>
                <form method="post" action="${a(p)}">
                    <input type="hidden" name="__RequestVerificationToken" value="${f()}" />
                    <button type="submit"
                            class="border-l border-gray-500 px-3 text-gray-400 hover:text-white hover:bg-red-800 transition h-full">✕</button>
                </form>
            </div>

            ${s?`<div id="answers-${e.questionId}" class="${l}">${g}</div>`:""}
        </div>`}function f(){var e;return((e=document.querySelector('input[name="__RequestVerificationToken"]'))==null?void 0:e.value)??""}function $(e,t,n){const r=v(t),o=r.get(u(null,null))??[];if(o.length===0){e.innerHTML=`
            <p class="text-gray-500 mb-4">Nog geen vragen toegevoegd.</p>
            <div class="drop-zone promote-zone border border-dashed border-gray-500 text-gray-400 text-center py-3 my-3 text-xs"
                 data-parent-id=""
                 data-parent-trigger="">
                Sleep een subvraag hierheen om er een hoofdvraag van te maken
            </div>`;return}const s=o.map(d=>c(d,0,r,n)).join("");e.innerHTML=`${s}
        <div class="drop-zone promote-zone border border-dashed border-gray-500 text-gray-400 text-center py-3 my-3 text-xs"
             data-parent-id=""
             data-parent-trigger="">
            Sleep een subvraag hierheen om er een hoofdvraag van te maken
        </div>`}export{$ as r};
