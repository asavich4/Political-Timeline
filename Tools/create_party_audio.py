"""Original game audio, synthesized offline; no sampled recordings or external music.
Run with Python + NumPy. WAV files are ordinary, replaceable Unity assets.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1] / 'PoliticalTimelineGame/Assets/Audio'
ROOT.mkdir(parents=True, exist_ok=True)
SR = 44100
BEAT = 60 / 84
COUNT = round(16 * 4 * BEAT * SR)
music = np.zeros((COUNT, 2), dtype=np.float64)

def hz(note):
    return 440 * 2 ** ((note - 69) / 12)

def add_note(note, start, beats, volume, pan=0, pad=False):
    t = np.arange(round(beats * BEAT * SR)) / SR
    f = hz(note)
    if pad:
        tone = np.sin(2*np.pi*f*t) + .18*np.sin(2*np.pi*2*f*t)
        env = np.sin(np.pi*np.arange(len(t))/len(t))**2
    else:
        tone = sum((1/k**1.7)*np.sin(2*np.pi*f*k*t)*np.exp(-t*k*.75) for k in range(1,6))
        env = (1-np.exp(-t*180))*np.exp(-t*1.6)
        env *= np.minimum(1, (t[-1]-t)/.06)
    sample = tone*env*volume
    idx = (round(start*BEAT*SR)+np.arange(len(t))) % COUNT
    music[idx,0] += sample*np.sqrt((1-pan)/2)
    music[idx,1] += sample*np.sqrt((1+pan)/2)

# D minor / Bb / F / C, then a restrained G minor / A turnaround.
chords = [[50,57,62,65],[46,53,58,62],[41,53,57,60],[48,55,60,64],
          [50,57,62,65],[46,53,58,62],[43,55,58,62],[45,57,61,64]]
melody = [[74,77,76],[74,70,69],[69,72,77],[76,72,67],
          [74,77,81],[77,74,70],[74,70,67],[73,76,69]]
for bar in range(16):
    chord=chords[bar%8]
    add_note(chord[0]-12,bar*4,4.5,.12,pad=True)
    for note in chord[1:]:
        add_note(note,bar*4,5,.027,pan=(note%3-1)*.25,pad=True)
    for step in range(4):
        add_note(chord[1+step%3],bar*4+step,2,.10 if step%2==0 else .065,pan=(-1 if step%2 else 1)*.32)
    for step,note in enumerate(melody[bar%8]):
        add_note(note,bar*4+[.5,2,3.25][step],2.7,.085 if bar<8 else .065,pan=.12)

# Circular room reflections preserve the tail across the exact musical loop boundary.
dry=music.copy()
for delay,gain in [(.113,.16),(.227,.11),(.379,.07),(.541,.045)]:
    music += np.roll(dry,round(delay*SR),axis=0)[:,::-1]*gain

def write(name, samples, peak):
    if samples.ndim==1: samples=samples[:,None]
    samples=samples*(peak/max(1e-9,np.max(np.abs(samples))))
    data=np.round(samples*32767).astype('<i2')
    with wave.open(str(ROOT/name),'wb') as out:
        out.setnchannels(data.shape[1]);out.setsampwidth(2);out.setframerate(SR);out.writeframes(data.tobytes())
    print(name, 'seconds',round(len(data)/SR,3),'peak',round(np.max(np.abs(samples)),3),
          'RMS',round(np.sqrt(np.mean(samples*samples)),4),'boundary step',round(float(np.max(np.abs(samples[0]-samples[-1]))),5))

write('BehindTheParty_Loop.wav',music,.65)
for index,duration in enumerate([.235,.27,.205,.17]):
    rng=np.random.default_rng(730+index)
    t=np.arange(round(SR*duration))/SR
    noise=rng.normal(0,1,len(t))
    body=np.convolve(noise,np.ones(15)/15,mode='same')
    paper=body-.6*np.convolve(body,np.ones(90)/90,mode='same')
    envelope=np.sin(np.pi*t/duration)**2.4
    flutter=1+.18*np.sin(2*np.pi*43*t)
    tick=np.sin(2*np.pi*175*t)*np.exp(-t*80)*(1-np.exp(-t*1000))
    sound=paper*envelope*flutter+.035*tick
    sound[:100]*=np.linspace(0,1,100);sound[-400:]*=np.linspace(1,0,400)
    name='Card_Confirm.wav' if index==3 else f'Card_Swipe_{index+1:02}.wav'
    write(name,sound,.48 if index==3 else .6)
