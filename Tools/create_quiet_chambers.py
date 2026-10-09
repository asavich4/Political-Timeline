"""Alternative original score: slow felt-key tones, low strings and sparse motifs.
No external samples. Python + NumPy; leaves the first score and card sounds intact.
"""
from pathlib import Path
import wave
import numpy as np

SR=44100
DURATION=64
N=SR*DURATION
mix=np.zeros((N,2))

def note(midi,start,duration,gain,pan=0,strings=False):
    t=np.arange(round(duration*SR))/SR
    f=440*2**((midi-69)/12)
    phase=2*np.pi*f*t
    if strings:
        # Soft low harmonics with gentle vibrato; long attack and release.
        phase+=.035*np.sin(2*np.pi*4.1*t)
        sound=np.sin(phase)+.13*np.sin(phase*2)+.045*np.sin(phase*3)
        env=np.sin(np.pi*np.arange(len(t))/len(t))**1.7
    else:
        # Rounded felt-key attack and a mellow, quickly decaying overtone.
        sound=np.sin(phase+.7*np.exp(-t*3)*np.sin(phase*2))
        sound+=.11*np.sin(phase*3)*np.exp(-t*4)
        env=(1-np.exp(-t*65))*np.exp(-t*.85)
        env*=np.minimum(1,(t[-1]-t)/.3)
    sample=sound*env*gain
    indices=(round(start*SR)+np.arange(len(t)))%N
    mix[indices,0]+=sample*np.sqrt((1-pan)/2)
    mix[indices,1]+=sample*np.sqrt((1+pan)/2)

# Slow minor/add-nine colors, eight seconds per harmony, avoiding a busy ostinato.
harmonies=[(40,[55,59,66]),(36,[55,59,64]),(43,[57,62,66]),(38,[57,60,64]),
           (40,[55,59,66]),(45,[55,60,64]),(36,[55,59,62]),(35,[54,57,64])]
motifs=[[(1.1,71),(5.4,66)],[(2.2,67)],[(.8,69),(6.1,66)],[(3.1,64)],
        [(1.6,71),(4.8,74)],[(2.7,72),(6.4,67)],[(1.2,71)],[(3.3,66),(6.1,64)]]
for section,(bass,chord) in enumerate(harmonies):
    start=section*8
    note(bass,start-.6,10,.10,strings=True)
    for index,pitch in enumerate(chord):
        note(pitch,start-.4+index*.07,9,.034,(index-1)*.42,strings=True)
    note(chord[0],start+.2,4,.095,-.22)
    note(chord[1],start+4.6,3.5,.06,.25)
    for onset,pitch in motifs[section]:
        note(pitch,start+onset,4.5,.074,.1)

dry=mix.copy()
for seconds,gain in [(.23,.18),(.47,.14),(.71,.09),(1.09,.055),(1.53,.03)]:
    mix+=np.roll(dry,round(seconds*SR),axis=0)[:,::-1]*gain
mix*=.60/np.max(np.abs(mix))
path=Path(__file__).resolve().parents[1]/'PoliticalTimelineGame/Assets/Audio/QuietChambers_Loop.wav'
with wave.open(str(path),'wb') as out:
    out.setnchannels(2);out.setsampwidth(2);out.setframerate(SR)
    out.writeframes(np.round(mix*32767).astype('<i2').tobytes())
assert np.isfinite(mix).all() and np.max(np.abs(mix))<1
print(f'{path.name}: {DURATION}s, peak {np.max(np.abs(mix)):.3f}, RMS {np.sqrt(np.mean(mix*mix)):.4f}, seam step {np.max(np.abs(mix[0]-mix[-1])):.5f}')
