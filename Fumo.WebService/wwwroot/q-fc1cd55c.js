import{c as h,n as _,$ as b,d as P,C as u,q as k,i as d,a as f}from"./q-087efa0f.js";import{u as A,a as C,g,b as $}from"./q-db81b645.js";const D=async(a,e)=>{const[t,o,r,n]=h();e.hasAttribute("preventdefault:click")&&(e.hasAttribute("q:nbs")?await t(location.href,{type:"popstate"}):e.href&&(e.setAttribute("aria-pressed","true"),await t(e.href,{forceReload:o,replaceState:r,scroll:n}),e.removeAttribute("aria-pressed")))},y=a=>{const e=A(),t=C(),{onClick$:o,reload:r,replaceState:n,scroll:v,...s}=(()=>a)(),i=_(()=>g({...s,reload:r},t)),l=_(()=>$(a,i,t));s["preventdefault:click"]=!!i,s.href=i||a.href;const c=l!=null?u(d(()=>f(()=>import("./q-9b892bb3.js"),["build/q-9b892bb3.js","build/q-db81b645.js","build/q-087efa0f.js"]),"s_eBQ0vFsFKsk")):void 0,p=u(d(()=>f(()=>Promise.resolve().then(()=>L),void 0),"s_i1Cv0pYJNR0",[e,r,n,v]));return b("a",{...s,children:P(k,null,3,"AD_0"),"data-prefetch":l,onClick$:[o,p],onFocus$:c,onMouseOver$:c,onQVisible$:c},null,0,"AD_1")},L=Object.freeze(Object.defineProperty({__proto__:null,s_8gdLBszqbaM:y,s_i1Cv0pYJNR0:D},Symbol.toStringTag,{value:"Module"}));export{y as s_8gdLBszqbaM,D as s_i1Cv0pYJNR0};
