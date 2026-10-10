import json, sys
def parse(txt):
    dec=json.JSONDecoder(); i=0; out=[]
    while i<len(txt):
        while i<len(txt) and txt[i].isspace(): i+=1
        if i>=len(txt): break
        o,j=dec.raw_decode(txt,i); out.append(o); i=j
    return out
def refs(x,acc):
    if isinstance(x,dict):
        if set(x.keys())=={"m_Id"} and isinstance(x["m_Id"],str): acc.append(x["m_Id"])
        else:
            for v in x.values(): refs(v,acc)
    elif isinstance(x,list):
        for v in x: refs(v,acc)
def check(path):
    objs=parse(open(path,encoding='utf-8').read())
    ids={o['m_ObjectId'] for o in objs if 'm_ObjectId' in o}
    errs=[]
    if len(ids)!=len([o for o in objs if 'm_ObjectId' in o]): errs.append('ids duplicados')
    for o in objs:
        acc=[]; refs(o,acc)
        for r in acc:
            if r!='' and r not in ids: errs.append('ref rota %s en %s'%(r,o.get('m_Type')))
    # edges: slot ids existen en el nodo
    by={o['m_ObjectId']:o for o in objs if 'm_ObjectId' in o}
    g=objs[0]
    for e in g['m_Edges']:
        for side in ('m_OutputSlot','m_InputSlot'):
            n=by[e[side]['m_Node']['m_Id']]; sid=e[side]['m_SlotId']
            sl=[by[s['m_Id']]['m_Id'] for s in n['m_Slots']]
            if sid not in sl: errs.append('slot %s no existe en %s'%(sid,n['m_Name']))
    return errs, len(objs)
if __name__=='__main__':
    for p in sys.argv[1:]:
        e,n=check(p); print(p.split('/')[-1], n,'objs', 'OK' if not e else e[:5])
