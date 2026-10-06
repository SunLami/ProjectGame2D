# Extend a Pixen-generated empty arena (mirror at niche center + loop plain floor segments). Reference copy; needs seam.py/extend.py below saved as modules.
"""=== seam.py ==="""
import os, numpy as np
from PIL import Image
T=os.environ['TEMP']
def load(k): return np.asarray(Image.open('_test/%s.png'%k).convert('RGB')).astype(np.float32)
def match(im,ref):
    out=im.copy()
    for c in range(3):
        out[...,c]=(im[...,c]-im[...,c].mean())/(im[...,c].std()+1e-6)*ref[...,c].std()+ref[...,c].mean()
    return np.clip(out,0,255)
def seam_path(A,B):
    # A,B: overlap strips (h,w,3), find vertical min-cost path; cost = diff + prefer dark grout
    d=((A-B)**2).sum(2)**0.5
    lum=(A.mean(2)+B.mean(2))/2
    cost=d+0.6*lum
    h,w=cost.shape; acc=cost.copy(); back=np.zeros((h,w),int)
    for y in range(1,h):
        for x in range(w):
            lo=max(0,x-1); hi=min(w,x+2); j=lo+np.argmin(acc[y-1,lo:hi]); back[y,x]=j; acc[y,x]+=acc[y-1,j]
    x=int(np.argmin(acc[-1])); path=[x]
    for y in range(h-1,0,-1): x=back[y,x]; path.append(x)
    return path[::-1]
def join_h(L,R,ov):
    # L right edge overlaps R left edge by ov px; heights equal
    h=L.shape[0]; A=L[:,-ov:]; B=R[:,:ov]
    p=seam_path(A,B)
    out=np.zeros((h,L.shape[1]+R.shape[1]-ov,3),np.float32)
    out[:,:L.shape[1]-ov]=L[:,:-ov]; out[:,L.shape[1]:]=R[:,ov:]
    for y in range(h):
        out[y,L.shape[1]-ov:L.shape[1]]=np.concatenate([A[y,:p[y]],B[y,p[y]:]],0)
    return out
def join_v(U,D,ov):
    return join_h(U.transpose(1,0,2),D.transpose(1,0,2),ov).transpose(1,0,2)
if __name__=='__main__':
    ims=[load(k) for k in 'abcd']; ref=ims[1]
    cw,ch=368,240
    crops=[]
    for im in ims:
        x=(448-cw)//2; y=(320-ch)//2
        crops.append(match(im[y:y+ch,x:x+cw],ref[y:y+ch,x:x+cw]))
    ov=48
    top=join_h(crops[0],crops[1],ov); bot=join_h(crops[2],crops[3],ov)
    full=join_v(top,bot,ov)
    Image.fromarray(np.clip(full,0,255).astype(np.uint8)).save(T+'/seam.png'); print(full.shape)
"""=== extend.py ==="""
import os, sys, numpy as np
from PIL import Image
sys.path.insert(0,os.environ['TEMP'])
import seam as S
T=os.environ['TEMP']
def find_loop(img,rng0,rng1,ov,minlen,axis):
    # axis 1: columns. find x0<x1 s.t. img[:,x1-ov:x1] ~ img[:,x0:x0+ov]
    A=img if axis==1 else img.transpose(1,0,2)
    best=(1e18,None)
    for x0 in range(rng0,rng1):
        for x1 in range(x0+minlen,rng1+minlen*2):
            if x1>A.shape[1]: break
            d=np.abs(A[:,x1-ov:x1]-A[:,x0:x0+ov]).mean()
            if d<best[0]: best=(d,(x0,x1))
    return best
def loop_extend(img,x0,x1,ov,k,axis):
    A=img if axis==1 else img.transpose(1,0,2)
    out=A[:,:x1]
    for _ in range(k): out=S.join_h(out,A[:,x0:x1],ov)
    out=np.concatenate([out,A[:,x1:]],1)
    return out if axis==1 else out.transpose(1,0,2)
if __name__=='__main__':
    img=np.asarray(Image.open(T+'/a402.png').convert('RGB')).astype(np.float32)
    H,W=img.shape[:2]; print(W,H)
    ov=10
    # horizontal: loop in left floor region (before niche) and right region
    d,(x0,x1)=find_loop(img,70,150,ov,70,1); print('H-left loop',d,x0,x1)
    img2=loop_extend(img,x0,x1,ov,2,1)
    Image.fromarray(img2.astype(np.uint8)).save(T+'/ext_h1.png'); print(img2.shape)
"""=== extend2.py (driver; current: k=6 horizontal loops, V loop fixed (212,298) x5) ==="""
import os, sys, numpy as np
from PIL import Image
sys.path.insert(0,os.environ['TEMP'])
import seam as S
from extend import find_loop, loop_extend
T=os.environ['TEMP']
img=np.asarray(Image.open(T+'/d701.png').convert('RGB')).astype(np.float32)
H,W=img.shape[:2]; xc=330
half=img[:,:xc]
ov=10
d,(x0,x1)=find_loop(half,90,200,ov,50,1); print('H loop',d,x0,x1,'len',x1-x0)
L=x1-x0; k=6; print('k',k,'adds',k*L)
half2=loop_extend(half,x0,x1,ov,k,1)
full=np.concatenate([half2,half2[:,::-1]],1); print('after H',full.shape)
# vertical: loop in floor rows
d,(y0,y1)=0,(212,298); print('V loop',d,y0,y1,'len',y1-y0)
Lv=y1-y0; kv=5; print('kv',kv,'adds',kv*Lv)
full2=loop_extend(full,y0,y1,ov,kv,0); print('final',full2.shape)
Image.fromarray(np.clip(full2,0,255).astype(np.uint8)).save(T+'/extended.png')
