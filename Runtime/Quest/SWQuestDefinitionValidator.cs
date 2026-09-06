using System;
using System.Collections.Generic;

namespace SW.Quest
{
    /// <summary>
    /// 퀘스트와 업적 정의의 공통 구조를 검증합니다.
    /// </summary>
    internal static class SWQuestDefinitionValidator
    {
        /// <summary>
        /// 정의 목록의 빈 참조, 코드명과 중복 항목을 검사합니다.
        /// </summary>
        /// <typeparam name="TQuest">검사할 퀘스트 정의 타입입니다.</typeparam>
        /// <param name="definitions">검사할 정의 목록입니다.</param>
        /// <param name="listName">검증 메시지에 표시할 목록 이름입니다.</param>
        /// <returns>발견한 문제 설명 목록입니다.</returns>
        internal static IReadOnlyList<string> Validate<TQuest>(
            IReadOnlyList<TQuest> definitions, string listName) where TQuest : SWQuest
        {
            List<string> messages = new();
            if (definitions == null)
            {
                messages.Add($"{listName} 목록이 비어 있습니다.");
                return messages;
            }

            HashSet<string> codeNames = new(StringComparer.Ordinal);
            for (int index = 0; index < definitions.Count; index++)
            {
                TQuest definition = definitions[index];
                if (definition == null)
                {
                    messages.Add($"{listName} 인덱스 {index}의 정의가 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.CodeName))
                {
                    messages.Add($"{listName} 코드명이 비어 있습니다: {definition.name}");
                }
                else if (!codeNames.Add(definition.CodeName))
                {
                    messages.Add($"{listName} 중복 코드명입니다: {definition.CodeName} ({definition.name})");
                }

                ValidateTaskGroups(definition, listName, messages);
            }

            return messages;
        }

        /// <summary>
        /// 퀘스트 정의의 작업 묶음, 작업 코드명과 묶음 내부 중복을 검사합니다.
        /// </summary>
        private static void ValidateTaskGroups(SWQuest definition, string listName,
            List<string> messages)
        {
            IReadOnlyList<SWQuestTaskGroup> taskGroups = definition.TaskGroups;
            if (taskGroups.Count == 0)
            {
                messages.Add($"{listName}에 작업 묶음이 없습니다: {definition.name}");
                return;
            }

            HashSet<string> taskGroupCodeNames = new(StringComparer.Ordinal);
            for (int groupIndex = 0; groupIndex < taskGroups.Count; groupIndex++)
            {
                SWQuestTaskGroup taskGroup = taskGroups[groupIndex];
                if (taskGroup == null)
                {
                    messages.Add($"{listName}의 작업 묶음이 비어 있습니다: {definition.name}, 묶음 {groupIndex}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(taskGroup.CodeName))
                {
                    messages.Add($"{listName}의 작업 묶음 코드명이 비어 있습니다: {definition.name}, 묶음 {groupIndex}");
                }
                else if (!taskGroupCodeNames.Add(taskGroup.CodeName))
                {
                    messages.Add($"{listName}의 작업 묶음 코드명이 중복됩니다: {definition.name}, 묶음 {taskGroup.CodeName}");
                }

                ValidateTasks(taskGroup, definition, listName, groupIndex, messages);
            }
        }

        /// <summary>
        /// 한 작업 묶음의 작업 코드명과 중복을 검사합니다.
        /// </summary>
        private static void ValidateTasks(SWQuestTaskGroup taskGroup, SWQuest definition,
            string listName, int groupIndex, List<string> messages)
        {
            IReadOnlyList<SWQuestTask> tasks = taskGroup.Tasks;
            if (tasks.Count == 0)
            {
                messages.Add($"{listName}의 작업 묶음에 작업이 없습니다: {definition.name}, 묶음 {groupIndex}");
                return;
            }

            HashSet<string> taskCodeNames = new(StringComparer.Ordinal);
            for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
            {
                SWQuestTask task = tasks[taskIndex];
                if (task == null)
                {
                    messages.Add($"{listName}의 작업이 비어 있습니다: {definition.name}, 묶음 {groupIndex}, 작업 {taskIndex}");
                }
                else if (string.IsNullOrWhiteSpace(task.CodeName))
                {
                    messages.Add($"{listName}의 작업 코드명이 비어 있습니다: {definition.name}, 묶음 {groupIndex}, 작업 {taskIndex}");
                }
                else if (!taskCodeNames.Add(task.CodeName))
                {
                    messages.Add($"{listName}의 한 묶음 안에서 작업 코드명이 중복됩니다: {definition.name}, 묶음 {groupIndex}, 작업 {task.CodeName}");
                }
            }
        }
    }
}
