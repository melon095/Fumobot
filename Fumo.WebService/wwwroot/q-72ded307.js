import{G as o}from"./q-db81b645.js";import{d as e,f as l,M as r,r as n,g as c}from"./q-087efa0f.js";import{useListLoader as p,useAddToListAction as d}from"./q-7b85c592.js";const m="_list_1ofyy_1",_="_empty_1ofyy_9",y="_input_1ofyy_22",h="_hint_1ofyy_32",t={list:m,empty:_,input:y,hint:h},g=()=>{const s=p(),i=d();return e(c,{children:[l("div",null,{class:"container container-center"},l("h1",null,null,[l("span",null,{class:"highlight"},"TODO",3,null)," List"],3,null),3,null),l("div",null,{class:"ellipsis",role:"presentation"},null,3,null),l("div",null,{class:"container container-center"},s.value.length===0?l("span",null,{class:t.empty},"No items found",3,"TN_0"):l("ul",null,{class:t.list},s.value.map((u,a)=>l("li",null,null,r(u,"text"),1,`items-${a}`)),1,null),1,null),l("div",null,{class:"container container-center"},[e(o,{action:i,children:[l("input",null,{class:t.input,name:"text",required:!0,type:"text"},null,3,null)," ",l("button",null,{class:"button-dark",type:"submit"},"Add item",3,null)],spaReset:!0,[n]:{action:n,spaReset:n}},3,"TN_1"),l("p",null,{class:t.hint},"PS: This little app works even when JavaScript is disabled.",3,null)],1,null)]},1,"TN_2")};export{g as s_52Swp3ilZe8};