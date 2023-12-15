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

        Raylib.InitWindow(screenWidth, screenHeight, "MIDI Player");

        Raylib.InitAudioDevice();
        Raylib.SetAudioStreamBufferSizeDefault(bufferSize);

        var stream = Raylib.LoadAudioStream((uint)sampleRate, 16, 2);
        var buffer = new short[2 * bufferSize];

        Raylib.PlayAudioStream(stream);

        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);
        var sequencer = new MidiFileSequencer(synthesizer);
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        sequencer.Play(midiFile, true);

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
            Raylib.ClearBackground(Raylib.LIGHTGRAY);
            Raylib.DrawText("FftFlat - A reasonably fast FFT in pure C#", 255, 200, 20, Raylib.DARKGRAY);

            for (var i = 0; i < barCount; i++)
            {
                var sum = 0.0;
                var startIndex = i * barWidth;
                var endIndex = startIndex + barWidth;
                for (var j = startIndex; j < endIndex; j++)
                {
                    sum += fftBuffer[j].Magnitude;
                }
                var b = 100 * Math.Log10(sum / barWidth + 0.000001) + 300;
                if (b > bars[i])
                {
                    bars[i] = 0.5 * bars[i] + 0.5 * b;
                }
                else
                {
                    bars[i] = 0.95 * bars[i] + 0.05 * b;
                }
                var h = (int)bars[i];
                Raylib.DrawRectangle(startIndex, screenHeight - h, barWidth, h, Raylib.RED);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
