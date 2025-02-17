import React from 'react';
import { Container } from 'reactstrap';
import NavMenu from '../NavMenu/NavMenu';

export default props => (
  <div>
    <NavMenu />
    <Container>
      {props.children}
    </Container>
  </div>
);
