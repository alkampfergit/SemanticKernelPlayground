# Semantic Kernel Experiments

Everything that is done with python uses the same virtual env

## Documentation

Nothing is better than code, here is the [official repo by Microsoft with examples](https://github.com/MicrosoftDocs/semantic-kernel-docs/blob/main/samples/python) 

The textual documentation can be found here: https://learn.microsoft.com/en-us/semantic-kernel/ai-orchestration/kernel/adding-services?tabs=python

## Installation

Create a local environment with python, then allow the ipykernel to create a kernel for jupyter notebooks.

```bash
python3 -m venv skernel
source skernel/bin/activate
# For windows you must use the following command to activate the virtual environment
#  .\skernel\Scripts\activate 
```

you can handle requirements with easy thanks to pip

```bash
pip install -r requirements.txt
pip freeze > requirements.txt
```

Then you can create a kernel for jupyter notebooks using the very same environmnent

```bash
pip install ipykernel
python -m ipykernel install --user --name=skernel_experiments
```

Kernel can be removed using 

```bash
jupyter kernelspec remove skernel_experiments
```

