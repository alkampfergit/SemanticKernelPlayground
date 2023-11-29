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