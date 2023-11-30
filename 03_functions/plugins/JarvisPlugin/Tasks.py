from semantic_kernel import SKContext
from semantic_kernel.skill_definition import sk_function, sk_function_context_parameter

class Tasks:

    @sk_function(
        description="Change the title of a task",
        name="ChangeTaskTitle"
    )
    @sk_function_context_parameter(
        name="task_id",
        description="id of the task that I want to update",
    )
    @sk_function_context_parameter(
        name="new_title",
        description="The new title of the task",
    )
    def change_task_title(self, context: SKContext) -> str:
        """
        Change the title of a task given id and task new title
        """
        task_id = context["task_id"]
        new_title = context["new_title"]

        print(f"Now call the api to change the title of the task {task_id} to {new_title}")

        # Api will return the new version of the task, so we can update the context
        return "7"

    @sk_function(
        description="Search task with full text string",
        name="SearchTask"
    )
    @sk_function_context_parameter(
        name="search_string",
        description="The string to search"
    )
    def search_task(self, search_string: str) -> str:
        print(f"Searching for task: {search_string}")
        # Here you should call the API to search for a task
        # api will return a list of tasks
        tasks = "Task_3, Task_4, Task_6"
        return tasks
    
    @sk_function(
        description="Load task detail in json format",
        name="LoadTask"
    )
    @sk_function_context_parameter(
        name="task_id",
        description="The id of the task to load"
    )  
    def load_task(self, task_id: str) -> str:
        """
        Load a task from the API and return the task content in json format
        """
        print(f"Loading task {task_id}")
        # Here you should call the API to load a task
        # api will return the task content
        tasks = {
            "Task_3": {
                "id": "Task_3",
                "title": "Hey I'm task 3",
                "description": "This is the description of task 3",
                "version": 5,
                "due_date": "2021-10-10"
            },
            "Task_4": {
                "id": "Task_4",
                "title": "I'm beautiful task 4",
                "description": "This is the description of task 4",
                "version": 5,
                "due_date": "2022-10-10"
            },
            "Task_6": {
                "id": "Task_6",
                "title": "I'm the six",
                "description": "This is the description of task 6",
                "version": 5,
                "due_date": "2023-10-10"
            }
        }
        task = tasks.get(task_id)
        # return task in json format
        return str(task)