using System;
using System.Linq;
using System.Numerics;
using FftFlat;
using MeltySynth;
using Raylib_CsLo;

class Program
{
    static readonly int screenWidth = 1024;
    static readonly int screenHeight = 768;

    static readonly int sampleRate = 44100;
    static readonly int bufferSize = 2048;

    static readonly Color color1 = new Color(179, 162, 199, 255);
    static readonly Color color2 = new Color(64, 49, 82, 255);

    static void Main()
    {
        var fftBuffer = new Complex[bufferSize];
        var window = new double[bufferSize];
        for (var i = 0; i < bufferSize; i++)
        {
            var x = 2 * Math.PI * i / bufferSize;
            window[i] = Math.Sqrt((1 - Math.Cos(x)) / 2);
        }
        var fft = new FastFourierTransform(bufferSize);

        var barWidth = 4;
        var barCount = screenWidth / barWidth;
        var bars = new double[barCount];

        Raylib.InitWindow(screenWidth, screenHeight, "FftFlat demo");

        Raylib.InitAudioDevice();
        Raylib.SetAudioStreamBufferSizeDefault(bufferSize);

        var stream = Raylib.LoadAudioStream((uint)sampleRate, 16, 2);
        var buffer = new short[2 * bufferSize];

        Raylib.PlayAudioStream(stream);

        var synthesizer = new Synthesizer("Arachno SoundFont - Version 1.0.sf2", sampleRate);
        synthesizer.MasterVolume = 2.5F;
        var sequencer = new MidiFileSequencer(synthesizer);
        var midiFile = new MidiFile("demo_song_for_arachno_soundfont.mid");
        sequencer.Play(midiFile, false);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsAudioStreamProcessed(stream))
            {
                sequencer.RenderInterleavedInt16(buffer);
                Raylib.UpdateAudioStream(stream, buffer.AsSpan(), bufferSize);

                for (var i = 0; i < bufferSize; i++)
                {
                    var s1 = buffer[2 * i] / 65536.0;
                    var s2 = buffer[2 * i + 1] / 65536.0;
                    fftBuffer[i] = window[i] * (s1 + s2);
                }
                fft.ForwardInplace(fftBuffer);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(color2);
            Raylib.DrawText("FftFlat - A fast FFT in pure C#", 90, 150, 50, color1);

            var prevIndex = 0;
            for (var i = 0; i < barCount; i++)
            {
                var a = (double)i / barCount * 0.8;
                var sum = 0.0;
                var index = (int)((bufferSize / 2) * Math.Pow(0.9 * a + 0.1, 2));
                for (var j = prevIndex; j <= index; j++)
                {
                    sum += fftBuffer[j].Magnitude;
                }
                prevIndex = index;

                var b = 200 * Math.Log10(sum / barWidth + 0.000001) + 300;
                if (b > bars[i])
                {
                    bars[i] = 0.5 * bars[i] + 0.5 * b;
                }
                else
                {
                    bars[i] = 0.95 * bars[i] + 0.05 * b;
                }
                var h = (int)bars[i];
                Raylib.DrawRectangle(i * barWidth, screenHeight - h, barWidth, h, color1);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
