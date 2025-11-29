// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function innerHtml(query)
{
    return Array.from(document.querySelectorAll(query)).map(el => el.innerHTML);
}
