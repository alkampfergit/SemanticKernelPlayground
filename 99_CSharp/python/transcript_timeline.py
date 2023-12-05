import whisper
import sys

def transcript_timeline(audiofile: str) -> str:
    model = whisper.load_model("tiny.en")

    transcription_options = {
        "verbose" : False,
        "task": "transcribe",
        "prompt": "You will transcribe the video to generate timeline for youtube"  # Add your prompt here
    }
    result = model.transcribe(audiofile, **transcription_options)

    transcription_string = ""
    for segment in result['segments']:
        start = segment['start']
        end = segment['end']
        text = segment['text']

        # Now I need to convert the start value, expressed in seconds, to 00:00 format
        # I can use the divmod function to get the minutes and seconds
        minutes, seconds = divmod(start, 60)
        transcription_string += f"{str(int(minutes)).zfill(2)}:{str(int(seconds)).zfill(2)}\t{text}\n"

    return transcription_string

if __name__ == "__main__":
    audio_path = sys.argv[1]
    result = transcript_timeline(audio_path)
    # simply print the result so the caller can grab with redirection
    print(result)