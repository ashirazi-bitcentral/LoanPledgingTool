﻿import React from 'react';
import { Router, Route } from 'react-router-dom';
import { connect } from 'react-redux';

import { history } from '../helpers';
import { alertActions } from '../actions';
import { PrivateRoute } from '../components';
import { HomePage } from '../HomePage';
import { LoginPage } from '../LoginPage';
import { PledgingPage } from '../LoanPledgingPage';
import { Alert } from 'reactstrap';

class App extends React.Component {
    constructor(props) {
        super(props);

        history.listen((location, action) => {
            this.props.clearAlerts();
        });

        this.state = {
            visible: true
        };

        this.onDismiss = this.onDismiss.bind(this);
    }

    onDismiss() {
        this.setState({ visible: false });
    }

    render() {
        const { alert } = this.props;
        return (

            <div>
                {alert.message &&
                <div>
                    <Alert color={alert.type} isOpen={this.state.visible} toggle={this.onDismiss} fade={false}>
                        {alert.message}
                    </Alert>
                </div>
                }
                <Router history={history}>
                    <div>
                        <PrivateRoute exact path="/" component={HomePage} />
                        <Route path="/login" component={LoginPage} />
                        <PrivateRoute path="/pledging" component={PledgingPage} />
                    </div>
                </Router>
            </div>
        );
    }
}

function mapState(state) {
    const { alert } = state;
    return { alert };
}

const actionCreators = {
    clearAlerts: alertActions.clear
};

const connectedApp = connect(mapState, actionCreators)(App);
export { connectedApp as App };