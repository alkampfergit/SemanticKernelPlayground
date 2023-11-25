import subprocess
from semantic_kernel.skill_definition import sk_function
import os
import whisper

class AudioVideo:

    @sk_function(
        description="extract audio in wav format from an mp4 file",
        name="ExtractAudio",
        input_description="Full path to the mp4 file",
    )
    def extract_audio(self, videofile: str) -> str:
        """
        Extract audio from a video file and return the full path to the extracted file.

        :param videofile: Full path to the mp4 file 
        :return: full path to the extracted audio file
        """
        # first of all change the extension to the video file to create output path
        audio_path = videofile.replace(".mp4", ".wav")
        command = f'ffmpeg -i {videofile} -vn -acodec pcm_s16le -ar 44100 -ac 2 {audio_path}'
        with open(os.devnull, 'w') as devnull:
            subprocess.call(command, shell=True, stdout=devnull, stderr=devnull)

        # now ffmpeg has created the audio file, return the path to it
        return audio_path

    @sk_function(
        description="Transcript audio from a wav file to a timeline",
        name="TranscriptTimeline",
        input_description="Full path to the wav file",
    )
    def transcript_timeline(self, audiofile: str) -> str:

        """
        Extract a transcript from an audio file and return a transcript file that
        contains for each line the start and end time of the audio segment and the
        transcripted text.
        :param audiofile: Full path to the wav file 
        :return: transcripted text with start and end time
        """
        model = whisper.load_model("base")

        transcription_options = {
            "task": "transcribe",
            "prompt": "You will transcribe the video to generate timeline for youtube"  # Add your prompt here
        }
        result = model.transcribe(audiofile, **transcription_options)

        transcription_string = ""
        for segment in result['segments']:
            start = segment['start']
            end = segment['end']
            text = segment['text']
            transcription_string += f"{start:.0f}\t{end:.0f}\t{text}\n"

        return transcription_string
