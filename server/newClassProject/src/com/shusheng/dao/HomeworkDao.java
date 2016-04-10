package com.shusheng.dao;

import java.util.List;

import com.shusheng.model.CommentEntity;
import com.shusheng.model.Homework;
import com.shusheng.model.Subject;

public interface HomeworkDao {

	/**
	 * 获取到该用户的所有作业
	 * @param username
	 * @param userStatus
	 * @return
	 */
	public List<Homework> getHomeworks(String username, int userStatus);

	/**
	 * 获取用户可以发布作业的科目
	 * @param username
	 * @param userStatus
	 * @return
	 */
	public List<String> getSubjects(String username, int userStatus);

	/**
	 * 存储一个作业
	 * @param homework
	 * @return
	 */
	public boolean save(Homework homework);

	/**
	 * 存储发布作业的科目信息
	 * @param subjectList
	 */
	public void saveSubject(List<Subject> subjectList);

	/**
	 * 存储一个评论信息
	 * @param commentEntity
	 */
	public void saveCommentEntity(CommentEntity commentEntity);

	
}
