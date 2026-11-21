"""Reads a prefab file: guid, root transform id, root GameObject id and root components (needed to author PrefabInstance mods)."""
import re, os, sys
from _paths import ASSETS as base
def scriptnames():
    d={}
    for dp,dn,fn in os.walk(os.path.join(base,"Scripts")):
        for f in fn:
            if f.endswith(".cs.meta"):
                g=re.search(r"guid: (\w+)",open(os.path.join(dp,f),encoding="utf8").read()).group(1)
                d[g]=f[:-8]
    return d
SN=scriptnames()
def info(rel):
    p=os.path.join(base,"Prefabs",rel)
    t=open(p,encoding="utf8").read()
    meta=re.search(r"guid: (\w+)",open(p+".meta",encoding="utf8").read()).group(1)
    blocks={}
    for blk in re.split(r"(?m)^--- ",t):
        m=re.match(r"!u!(\d+) &(\d+)",blk)
        if m: blocks[m.group(2)]=(m.group(1),blk)
    rootT=[i for i,(k,b) in blocks.items() if k in("4","224") and re.search(r"m_Father: \{fileID: 0\}",b)][0]
    rootGO=re.search(r"m_GameObject: \{fileID: (\d+)\}",blocks[rootT][1]).group(1)
    comps=re.findall(r"- component: \{fileID: (\d+)\}",blocks[rootGO][1])
    out=[]
    for c in comps:
        k,b=blocks[c]
        if k=="114":
            g=re.search(r"m_Script: \{fileID: \d+, guid: (\w+)",b).group(1)
            out.append((c,"MB:"+SN.get(g,g)))
        else: out.append((c,"type"+k))
    return meta,rootT,rootGO,out
if __name__=="__main__":
    for rel in sys.argv[1:]:
        print(rel,info(rel))
