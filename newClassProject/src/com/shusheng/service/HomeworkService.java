package com.shusheng.service;

import java.util.List;

import com.shusheng.model.CommentEntity;
import com.shusheng.model.Homework;
import com.shusheng.model.Subject;

public interface HomeworkService {

	/**
	 * 添加一次作业
	 * @param homework
	 * @return
	 */
	public boolean addHomework(Homework homework);

	/**
	 * 获取到该用户所对应的作业内容
	 * @param username
	 * @param userStatus
	 * @return
	 */
	public List<Homework> getHomeworks(String username, int userStatus);

	/**
	 * 通过用户名和身份获取发布作业的科目
	 * @param username
	 * @param userStatus
	 * @return
	 */
	public List<String> getSubjects(String username, int userStatus);

	
	/**
	 * 存储发布作业的科目
	 * @param subjectList
	 */
	public void saveSubjects(List<Subject> subjectList);

	/**
	 * 添加一个评论信息
	 * @param commentEntity
	 */
	public void saveCommentEntity(CommentEntity commentEntity);
}
