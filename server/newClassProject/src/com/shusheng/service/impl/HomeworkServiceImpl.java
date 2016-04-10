package com.shusheng.service.impl;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import com.shusheng.dao.HomeworkDao;
import com.shusheng.model.CommentEntity;
import com.shusheng.model.Homework;
import com.shusheng.model.Subject;
import com.shusheng.service.HomeworkService;

@Component("homeworkService")
public class HomeworkServiceImpl implements HomeworkService {

	@Autowired
	private HomeworkDao homeworkDao;
	/**
	 * 添加一此作业
	 */
	@Override
	public boolean addHomework(Homework homework) {
		return homeworkDao.save(homework);
	}

	@Override
	public List<Homework> getHomeworks(String username, int userStatus) {
		return homeworkDao.getHomeworks(username,userStatus);
	}

	@Override
	public List<String> getSubjects(String username, int userStatus) {
		return homeworkDao.getSubjects(username,userStatus);
	}

	/**
	 * 存储发布的作业的科目
	 */
	@Override
	public void saveSubjects(List<Subject> subjectList) {
		homeworkDao.saveSubject(subjectList);
		
	}

	/**
	 * 添加一个评论信息
	 */
	@Override
	public void saveCommentEntity(CommentEntity commentEntity) {
		homeworkDao.saveCommentEntity(commentEntity);
		
	}

}
