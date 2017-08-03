# Skills Set Import

A skill set comprises of a list of skills that are related to one or more courses. Skills in the set can be tagged with any adhoc label, these tags can then be used to display and filter the skills. 

The import process is currently done via a csv file and a simple admin screen. Future developments should see full admin screens for administering skills and tags.

For now you can use the template file to complete your skills and then import it at [localhost api address]/admin/importskills.

> Note: The import page only works on locally on the server for security.

Before you attempt the import you will need to create at least 1 Course row in the database.
After the import you will need to set the type of the imported taxonomy's (tag groups):
- 0 = Unknown
- 1 = SkillsHierarchies
- 2 = SkillsFilters

The Skill Hierarchy taxonomy will be used to group the display of the skills, the Skill Filter will be used to filter the skills.

| CSV Line | Description |
| -------- | ----------- |
| 1. | SELF ASSESSMENT SCALES – Heading, please leave. |
| 2. | |
| a. | Name – Heading, please leave |
| b. | Levels – Heading, please leave |
| 3. | |
| a. | Original 7 Point - This is the name of a Self Assessment Scale, it is referenced later within the data. If you wish it use another type of scale you can rename – or alternatively add another Name in the row below. You can have as many scales as you want. |
| b. | ‘Little or no understanding’ - This is the first level of the 7 point scale, it carries on through to column h. ‘Competent to perform independently’ |
| 4. | SKILLS LIST– Heading, please leave |
| 5. | This row contains the headings, please leave. |
| a. | Skills – This column is used for the name of the skills |
| b. | Parent Skill – This column has the name of the Parent Skills (if they have one). |
| c. | Can Self Assess – This column indicates if the skill can be self assessed i.e. if it has the self assessment slider shown.  Only skills with no parent can be self assessed. |
| d. | Self Assessment Scale – This column has the name of the scale that you want to use (if required) for the self assess. In our example there is only the Original 7 Point. |
| e. | Can Self Count – This column is no longer used, please just leave empty. |
| f. | Categories – From this column onwards is where you put the tags for the skills. They are entered in the format category/tag e.g. AVMA/Competency 8 this will tag the skill as Competency 8 in the AVMA category. One of the category must be used to group your skills together when showing them in the app. We have named ours Hierarchy, you will see we have further nested this category to three deep e.g. Hierarchy/Professional Practice/Communication. If you need to have a forward slash (/) in the name you will have to escape it with another forward slash e.g. Species/Bird//Reptile//Exotics. You will notice that you can have gaps in the columns, this often happens when some skills are tagged in more categories than others and then you start a new category in a new column. |
| 6. | From this row onwards is where our skill data is listed. |